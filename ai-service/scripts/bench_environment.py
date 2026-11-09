"""
Task 1.3 — Đo môi trường AI Service (RAM, VRAM, cold start, độ trễ).

Cách chạy (BẮT BUỘC từ thư mục ai-service):
    .\\venv\\Scripts\\python.exe scripts\\bench_environment.py
    .\\venv\\Scripts\\python.exe scripts\\bench_environment.py --runs 30 --warmup 5
    .\\venv\\Scripts\\python.exe scripts\\bench_environment.py --json benchmarks\\environment.json --markdown benchmarks\\environment.md

Nguyên tắc đo:
- LLM luôn được mock (FakeLLMClient) => không phụ thuộc mạng, không tốn token.
- Có warm-up trước khi đo; báo cáo median / p95 / min / max thay vì một con số duy nhất.
- Cold start đo bằng tiến trình Python MỚI (subprocess) để không bị nhiễu bởi import cache.
- Không ghi đè bất kỳ file nào ngoài thư mục chỉ định ở --json/--markdown.
"""

from __future__ import annotations

import argparse
import asyncio
import json
import os
import platform
import statistics
import subprocess
import sys
import time
from dataclasses import asdict, dataclass, field
from pathlib import Path
from typing import Any

AI_SERVICE_DIR = Path(__file__).resolve().parents[1]
# Cho phép chạy `python scripts/bench_environment.py` từ thư mục ai-service.
if str(AI_SERVICE_DIR) not in sys.path:
    sys.path.insert(0, str(AI_SERVICE_DIR))

ADVISORY_PAYLOAD = {
    "requestId": "b3f1c2a0-1234-4a5b-9c1d-abcdef123456",
    "device": {"deviceType": "phone", "brand": "iPhone", "model": "13"},
    "issueDescription": "Pin tụt nhanh, máy nóng khi sạc",
    "context": {
        "availableServices": [
            {
                "serviceId": "svc-1",
                "name": "Thay pin",
                "deviceType": "phone",
                "basePrice": 400000,
            }
        ],
        "availableParts": [
            {"partId": "part-1", "name": "Pin iPhone 13", "unitPrice": 350000}
        ],
    },
}

FAKE_LLM_OUTPUT = {
    "outOfScope": False,
    "suggestedServices": [{"serviceId": "svc-1", "confidence": "HIGH"}],
    "suggestedParts": [{"partId": "part-1"}],
    "priceMin": 300000,
    "priceMax": 500000,
    "reason": "Pin tụt nhanh là dấu hiệu pin chai.",
}


class FakeLLMClient:
    """Thay LLM thật để đo phần hạ tầng (không đo chất lượng sinh văn bản)."""

    async def generate_json(self, system_prompt: str, user_prompt: str) -> dict:
        return FAKE_LLM_OUTPUT


@dataclass
class LatencyStats:
    runs: int = 0
    median_ms: float = 0.0
    p95_ms: float = 0.0
    min_ms: float = 0.0
    max_ms: float = 0.0
    mean_ms: float = 0.0


@dataclass
class BenchResult:
    generated_at: str = ""
    environment: dict[str, Any] = field(default_factory=dict)
    resources: dict[str, Any] = field(default_factory=dict)
    cold_start: dict[str, Any] = field(default_factory=dict)
    latency: dict[str, Any] = field(default_factory=dict)
    notes: list[str] = field(default_factory=list)


def _percentile(values: list[float], ratio: float) -> float:
    """Percentile nội suy tuyến tính (đủ dùng cho n nhỏ, không cần numpy)."""
    if not values:
        return 0.0
    ordered = sorted(values)
    if len(ordered) == 1:
        return ordered[0]
    position = (len(ordered) - 1) * ratio
    lower = int(position)
    upper = min(lower + 1, len(ordered) - 1)
    weight = position - lower
    return ordered[lower] * (1 - weight) + ordered[upper] * weight


def _stats(samples_ms: list[float]) -> LatencyStats:
    return LatencyStats(
        runs=len(samples_ms),
        median_ms=round(statistics.median(samples_ms), 3) if samples_ms else 0.0,
        p95_ms=round(_percentile(samples_ms, 0.95), 3),
        min_ms=round(min(samples_ms), 3) if samples_ms else 0.0,
        max_ms=round(max(samples_ms), 3) if samples_ms else 0.0,
        mean_ms=round(statistics.fmean(samples_ms), 3) if samples_ms else 0.0,
    )


def _run_python_snippet(code: str) -> dict[str, Any]:
    """Chạy 1 snippet trong tiến trình Python mới; trả về stdout/returncode (không raise)."""
    started = time.perf_counter()
    completed = subprocess.run(
        [sys.executable, "-c", code],
        cwd=str(AI_SERVICE_DIR),
        capture_output=True,
        text=True,
        timeout=180,
    )
    wall_ms = (time.perf_counter() - started) * 1000
    return {
        "returncode": completed.returncode,
        "stdout": completed.stdout.strip(),
        "stderr": completed.stderr.strip()[-500:],
        "wall_ms": round(wall_ms, 3),
    }


def collect_environment() -> dict[str, Any]:
    import importlib.metadata as metadata

    def version(package: str) -> str | None:
        try:
            return metadata.version(package)
        except metadata.PackageNotFoundError:
            return None

    from app.core.config import settings

    return {
        "python": sys.version.split()[0],
        "executable": sys.executable,
        "platform": platform.platform(),
        "machine": platform.machine(),
        "processor": platform.processor(),
        "logicalCpus": os.cpu_count(),
        "packages": {
            "fastapi": version("fastapi"),
            "uvicorn": version("uvicorn"),
            "pydantic": version("pydantic"),
            "pydantic-settings": version("pydantic-settings"),
            "httpx": version("httpx"),
            "pytest": version("pytest"),
            "pytest-asyncio": version("pytest-asyncio"),
            "langgraph": version("langgraph"),
            "scikit-learn": version("scikit-learn"),
            "numpy": version("numpy"),
            "psutil": version("psutil"),
        },
        "settings": {
            "appEnv": settings.app_env,
            "llmMode": getattr(settings, "llm_mode", "ollama"),
            "llmModel": settings.llm_model,
            "llmBaseUrlHost": _safe_host(settings.llm_base_url),
            "llmApiKeyConfigured": bool(settings.llm_api_key),
            "requestTimeoutSeconds": settings.request_timeout_seconds,
        },
    }


def _safe_host(url: str) -> str:
    """Chỉ lấy host của base URL — không bao giờ in key/query string."""
    without_scheme = url.split("://", 1)[-1]
    return without_scheme.split("/", 1)[0]


def _process_rss_mb() -> float | None:
    try:
        import psutil

        return round(psutil.Process().memory_info().rss / (1024 * 1024), 1)
    except Exception:
        return None


def collect_resources() -> dict[str, Any]:
    result: dict[str, Any] = {}

    try:
        import psutil

        process = psutil.Process()
        result["processRssMb"] = round(process.memory_info().rss / (1024 * 1024), 1)
        virtual = psutil.virtual_memory()
        result["systemRamTotalMb"] = round(virtual.total / (1024 * 1024), 1)
        result["systemRamAvailableMb"] = round(virtual.available / (1024 * 1024), 1)
        result["memoryMeasurement"] = "psutil"
    except Exception as exc:  # pragma: no cover - chỉ chạy khi thiếu psutil
        result["memoryMeasurement"] = f"unavailable ({type(exc).__name__})"

    gpu = _query_nvidia_smi()
    result.update(gpu)
    return result


def _query_nvidia_smi() -> dict[str, Any]:
    query = "name,memory.total,memory.used,driver_version"
    try:
        completed = subprocess.run(
            [
                "nvidia-smi",
                f"--query-gpu={query}",
                "--format=csv,noheader,nounits",
            ],
            capture_output=True,
            text=True,
            timeout=20,
        )
    except (FileNotFoundError, subprocess.SubprocessError) as exc:
        return {"gpuAvailable": False, "gpuDetail": type(exc).__name__}

    if completed.returncode != 0 or not completed.stdout.strip():
        return {"gpuAvailable": False, "gpuDetail": completed.stderr.strip()[:200]}

    fields = [part.strip() for part in completed.stdout.strip().splitlines()[0].split(",")]
    if len(fields) < 4:
        return {"gpuAvailable": False, "gpuDetail": "unexpected nvidia-smi output"}

    total_mb = float(fields[1])
    used_mb = float(fields[2])
    return {
        "gpuAvailable": True,
        "gpuName": fields[0],
        "gpuDriver": fields[3],
        "vramTotalMb": total_mb,
        "vramUsedMb": used_mb,
        "vramFreeMb": round(total_mb - used_mb, 1),
    }


def measure_cold_start() -> dict[str, Any]:
    """Cold start = thời gian import app.main trong tiến trình Python mới."""
    app_import = _run_python_snippet(
        "import time; t=time.perf_counter(); import app.main; "
        "print(f'{(time.perf_counter()-t)*1000:.1f}')"
    )
    langgraph_import = _run_python_snippet(
        "import time; t=time.perf_counter(); import langgraph.graph; "
        "print(f'{(time.perf_counter()-t)*1000:.1f}')"
    )
    sklearn_import = _run_python_snippet(
        "import time; t=time.perf_counter(); import sklearn; "
        "print(f'{(time.perf_counter()-t)*1000:.1f}')"
    )
    graph_compile = _run_python_snippet(
        "import time\n"
        "from typing import TypedDict\n"
        "from langgraph.graph import END, START, StateGraph\n"
        "\n"
        "class S(TypedDict, total=False):\n"
        "    x: int\n"
        "\n"
        "def node(state: S) -> S:\n"
        "    return state\n"
        "\n"
        "started = time.perf_counter()\n"
        "graph = StateGraph(S)\n"
        "graph.add_node('a', node)\n"
        "graph.add_edge(START, 'a')\n"
        "graph.add_edge('a', END)\n"
        "graph.compile()\n"
        "print(f'{(time.perf_counter() - started) * 1000:.1f}')\n"
    )

    def value_of(snippet: dict[str, Any]) -> float | None:
        if snippet["returncode"] != 0:
            return None
        try:
            return float(snippet["stdout"].splitlines()[-1])
        except (ValueError, IndexError):
            return None

    return {
        "importAppMainMs": value_of(app_import),
        "importLanggraphMs": value_of(langgraph_import),
        "importSklearnMs": value_of(sklearn_import),
        "compileTrivialGraphMs": value_of(graph_compile),
        "errors": {
            name: snippet["stderr"]
            for name, snippet in {
                "app.main": app_import,
                "langgraph": langgraph_import,
                "sklearn": sklearn_import,
                "compileGraph": graph_compile,
            }.items()
            if snippet["returncode"] != 0
        },
    }


async def _measure_service_latency(runs: int) -> list[float]:
    from app.schemas.request_models import AdvisoryRequest
    from app.services.advisory_service import AdvisoryService

    service = AdvisoryService(client=FakeLLMClient())
    request = AdvisoryRequest.model_validate(ADVISORY_PAYLOAD)

    samples: list[float] = []
    for _ in range(runs):
        started = time.perf_counter()
        await service.get_advisory(request)
        samples.append((time.perf_counter() - started) * 1000)
    return samples


def measure_latency(runs: int, warmup: int) -> dict[str, Any]:
    from fastapi.testclient import TestClient

    from app.core.config import settings
    from app.main import app
    from app.services.advisory_service import advisory_service

    # Mock LLM cho cả service dùng bởi endpoint (không gọi mạng).
    advisory_service._llm_client = FakeLLMClient()  # type: ignore[attr-defined]

    asyncio.run(_measure_service_latency(warmup))  # warm-up

    service_samples = asyncio.run(_measure_service_latency(runs))

    client = TestClient(app)
    headers = {"X-Internal-Api-Key": settings.internal_api_key}

    endpoint_samples: list[float] = []
    for _ in range(runs):
        started = time.perf_counter()
        response = client.post("/v1/advisory", json=ADVISORY_PAYLOAD, headers=headers)
        endpoint_samples.append((time.perf_counter() - started) * 1000)
        assert response.status_code == 200, response.text

    health_started = time.perf_counter()
    health = client.get("/health")
    health_ms = (time.perf_counter() - health_started) * 1000

    deep_started = time.perf_counter()
    deep = client.get("/health/deep")
    deep_ms = (time.perf_counter() - deep_started) * 1000

    return {
        "serviceOnly": asdict(_stats(service_samples)),
        "httpAdvisory": asdict(_stats(endpoint_samples)),
        "health": {
            "healthMs": round(health_ms, 3),
            "healthDeepMs": round(deep_ms, 3),
            "healthStatus": health.status_code,
            "healthDeepStatus": deep.status_code,
        },
        "rawSamplesMs": {
            "serviceOnly": [round(sample, 3) for sample in service_samples],
            "httpAdvisory": [round(sample, 3) for sample in endpoint_samples],
        },
    }


def build_markdown(result: BenchResult) -> str:
    env = result.environment
    resources = result.resources
    cold = result.cold_start
    latency = result.latency

    def fmt(value: Any) -> str:
        if value is None:
            return "n/a"
        if isinstance(value, float):
            return f"{value:.1f}"
        return str(value)

    lines = [
        "# Kết quả đo môi trường AI Service (Task 1.3)",
        "",
        f"- Thời điểm đo: {result.generated_at}",
        f"- Python: {env.get('python')} — {env.get('platform')}",
        f"- CPU logic: {env.get('logicalCpus')}",
        f"- LLM mode: {env.get('settings', {}).get('llmMode')} / model: {env.get('settings', {}).get('llmModel')}",
        "",
        "## Tài nguyên",
        "",
        "| Hạng mục | Giá trị |",
        "|---|---|",
        f"| RSS tiến trình bench | {fmt(resources.get('processRssMb'))} MB |",
        f"| RSS sau khi import app.main + chạy advisory | {fmt(resources.get('processRssAfterAppImportMb'))} MB |",
        f"| RAM hệ thống (total / available) | {fmt(resources.get('systemRamTotalMb'))} / {fmt(resources.get('systemRamAvailableMb'))} MB |",
        f"| GPU | {resources.get('gpuName', 'n/a')} |",
        f"| VRAM total / used / free | {fmt(resources.get('vramTotalMb'))} / {fmt(resources.get('vramUsedMb'))} / {fmt(resources.get('vramFreeMb'))} MB |",
        "",
        "## Cold start (tiến trình mới)",
        "",
        "| Hạng mục | ms |",
        "|---|---|",
        f"| import app.main | {fmt(cold.get('importAppMainMs'))} |",
        f"| import langgraph.graph | {fmt(cold.get('importLanggraphMs'))} |",
        f"| import sklearn | {fmt(cold.get('importSklearnMs'))} |",
        f"| compile StateGraph tối giản | {fmt(cold.get('compileTrivialGraphMs'))} |",
        "",
        "## Độ trễ (LLM mock, không gọi mạng)",
        "",
        "| Luồng | n | median | p95 | min | max |",
        "|---|---|---|---|---|---|",
    ]

    for name, key in (("AdvisoryService (in-process)", "serviceOnly"), ("POST /v1/advisory (TestClient)", "httpAdvisory")):
        stats = latency.get(key, {})
        lines.append(
            f"| {name} | {stats.get('runs', 0)} | {fmt(stats.get('median_ms'))} | "
            f"{fmt(stats.get('p95_ms'))} | {fmt(stats.get('min_ms'))} | {fmt(stats.get('max_ms'))} |"
        )

    health = latency.get("health", {})
    lines += [
        "",
        f"- `GET /health`: {fmt(health.get('healthMs'))} ms (HTTP {health.get('healthStatus')})",
        f"- `GET /health/deep`: {fmt(health.get('healthDeepMs'))} ms (HTTP {health.get('healthDeepStatus')})",
        "",
        "## Ghi chú",
        "",
    ]
    lines += [f"- {note}" for note in result.notes]
    return "\n".join(lines) + "\n"


def main() -> int:
    # Console Windows mặc định cp1252 khi bị redirect -> ép UTF-8 để in tiếng Việt không lỗi.
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")

    parser = argparse.ArgumentParser(description="Đo môi trường AI Service (Task 1.3)")
    parser.add_argument("--runs", type=int, default=20, help="Số lần đo độ trễ (sau warm-up)")
    parser.add_argument("--warmup", type=int, default=3, help="Số lần warm-up")
    parser.add_argument("--json", type=Path, default=None, help="Ghi kết quả JSON")
    parser.add_argument("--markdown", type=Path, default=None, help="Ghi báo cáo Markdown")
    args = parser.parse_args()

    from datetime import datetime, timezone

    result = BenchResult(generated_at=datetime.now(timezone.utc).isoformat(timespec="seconds"))
    result.environment = collect_environment()
    result.resources = collect_resources()
    result.cold_start = measure_cold_start()
    result.latency = measure_latency(args.runs, args.warmup)
    result.resources["processRssAfterAppImportMb"] = _process_rss_mb()

    result.notes = [
        "LLM được mock (FakeLLMClient) nên số đo phản ánh hạ tầng, KHÔNG phải chất lượng sinh văn bản.",
        "Cold start đo bằng subprocess Python mới; lần chạy thứ hai sẽ nhanh hơn do OS cache.",
        f"Đo {args.runs} lần sau {args.warmup} lần warm-up; p95 tính bằng nội suy tuyến tính.",
        "VRAM lấy từ nvidia-smi tại thời điểm đo, không phải VRAM riêng của tiến trình AI Service.",
    ]
    if result.cold_start.get("errors"):
        result.notes.append(f"Có lỗi khi đo cold start: {result.cold_start['errors']}")

    markdown = build_markdown(result)
    if args.json:
        args.json.parent.mkdir(parents=True, exist_ok=True)
        args.json.write_text(
            json.dumps(asdict(result), ensure_ascii=False, indent=2), encoding="utf-8"
        )
    if args.markdown:
        args.markdown.parent.mkdir(parents=True, exist_ok=True)
        args.markdown.write_text(markdown, encoding="utf-8")

    print(markdown)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
