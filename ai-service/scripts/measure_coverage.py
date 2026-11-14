"""
Python Stdlib Line Coverage Measurement Script (TASK-15).

Sử dụng Python stdlib `trace` + `dis` + `threading.settrace` để đo độ phủ dòng (line coverage)
trên toàn bộ mã nguồn `ai-service/app/**/*.py` khi chạy bộ test `pytest ai-service/tests -q`.

Lưu ý quan trọng: Script tuyệt đối không import bất kỳ module `app.*` nào trước khi bật tracer
để đảm bảo các câu lệnh top-level/class/import được ghi nhận đầy đủ khi pytest load module.

Chạy:
    $env:INTERNAL_API_KEY="test-key-123"
    $env:PYTHONPATH="ai-service;ai-service\\venv\\Lib\\site-packages"
    C:\\Apps\\python.exe ai-service\\scripts\\measure_coverage.py
"""

from __future__ import annotations

import ast
import dis
import json
import os
import sys
import threading
import trace
import types
from datetime import datetime, timezone
from pathlib import Path
from typing import Any, Dict, List, Set

import pytest

AI_SERVICE_DIR = Path(__file__).resolve().parents[1]
APP_DIR = AI_SERVICE_DIR / "app"
TESTS_DIR = AI_SERVICE_DIR / "tests"
OUTPUT_PATH = APP_DIR / "ml" / "models" / "coverage_metrics.json"


def _normalize_path(path_str: str) -> Path:
    return Path(os.path.normcase(os.path.abspath(path_str)))


def _type_checking_lines(source: str) -> Set[int]:
    """Loại trừ khối `if TYPE_CHECKING:` vì không bao giờ thực thi lúc runtime."""
    excluded: Set[int] = set()
    try:
        tree = ast.parse(source)
    except SyntaxError:
        return excluded
    for node in ast.walk(tree):
        if isinstance(node, ast.If):
            test = node.test
            is_tc = (isinstance(test, ast.Name) and test.id == "TYPE_CHECKING") or (
                isinstance(test, ast.Attribute) and test.attr == "TYPE_CHECKING"
            )
            if is_tc and node.body:
                start = node.lineno
                end = max(getattr(n, "end_lineno", start) or start for n in node.body)
                excluded.update(range(start, end + 1))
    return excluded


def _executable_lines(py_file: Path) -> Set[int]:
    """Trích xuất tập các dòng thực thi thực sự từ bytecode (`dis.findlinestarts`)."""
    source = py_file.read_text(encoding="utf-8")
    if not source.strip():
        return set()
    code_obj = compile(source, str(py_file), "exec")
    lines: Set[int] = set()

    def _collect(co: types.CodeType) -> None:
        for _, lineno in dis.findlinestarts(co):
            if lineno is not None and lineno > 0:
                lines.add(int(lineno))
        for const in co.co_consts:
            if isinstance(const, types.CodeType):
                _collect(const)

    _collect(code_obj)
    # Remove docstring-only line or TYPE_CHECKING blocks; keep executed lines unioned later
    lines -= _type_checking_lines(source)
    return lines


def main() -> int:
    os.environ.setdefault("INTERNAL_API_KEY", "test-key-123")
    if str(AI_SERVICE_DIR) not in sys.path:
        sys.path.insert(0, str(AI_SERVICE_DIR))

    tracer = trace.Trace(
        count=True,
        trace=False,
        ignoredirs=[sys.prefix, sys.exec_prefix],
    )

    exit_code = 1
    threading.settrace(tracer.globaltrace)
    try:
        exit_code = tracer.runfunc(pytest.main, [str(TESTS_DIR), "-q"])
    finally:
        threading.settrace(None)

    results = tracer.results()
    hits_by_file: Dict[Path, Set[int]] = {}
    for (filename, lineno), count in results.counts.items():
        if count > 0 and filename:
            norm = _normalize_path(filename)
            hits_by_file.setdefault(norm, set()).add(int(lineno))

    app_files = sorted(APP_DIR.rglob("*.py"))
    file_reports: List[Dict[str, Any]] = []
    package_totals: Dict[str, Dict[str, int]] = {}

    total_stmts = 0
    total_covered = 0

    for py_file in app_files:
        norm = _normalize_path(str(py_file))
        exec_lines = _executable_lines(py_file)
        hit_lines = hits_by_file.get(norm, set()) & exec_lines
        uncovered = sorted(exec_lines - hit_lines)

        stmts = len(exec_lines)
        cov = len(hit_lines)
        pct = round((cov / stmts) * 100.0, 2) if stmts > 0 else 100.0

        rel = py_file.relative_to(AI_SERVICE_DIR).as_posix()
        parts = py_file.relative_to(APP_DIR).parts
        pkg = f"app/{parts[0]}" if len(parts) > 1 else "app"

        bucket = package_totals.setdefault(pkg, {"statements": 0, "covered": 0})
        bucket["statements"] += stmts
        bucket["covered"] += cov

        total_stmts += stmts
        total_covered += cov

        file_reports.append({
            "file": rel,
            "package": pkg,
            "statements": stmts,
            "covered": cov,
            "missed": len(uncovered),
            "coveragePct": pct,
            "uncoveredSampleLines": uncovered[:20],
        })

    overall_pct = round((total_covered / total_stmts) * 100.0, 2) if total_stmts > 0 else 0.0
    package_summary = {
        pkg: {
            "statements": vals["statements"],
            "covered": vals["covered"],
            "coveragePct": round((vals["covered"] / vals["statements"]) * 100.0, 2)
            if vals["statements"] > 0
            else 100.0,
        }
        for pkg, vals in sorted(package_totals.items())
    }

    report = {
        "generatedAtUtc": datetime.now(timezone.utc).isoformat(),
        "tool": "python-stdlib-trace+dis",
        "pytestExitCode": int(exit_code),
        "totalStatements": total_stmts,
        "totalCovered": total_covered,
        "totalMissed": total_stmts - total_covered,
        "overallLineCoveragePct": overall_pct,
        "targetPct": 85.0,
        "meetsTarget": overall_pct >= 85.0,
        "byPackage": package_summary,
        "files": file_reports,
    }
    OUTPUT_PATH.write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")

    print("\n=== PYTHON LINE COVERAGE SUMMARY (ai-service/app) ===")
    print(f"pytest exit code: {exit_code}")
    print(f"Total executable lines: {total_stmts}, Covered: {total_covered}, Missed: {total_stmts - total_covered}")
    print(f"Overall Line Coverage: {overall_pct:.2f}% (Target >= 85.0%)")
    for pkg, stats in package_summary.items():
        print(f"  {pkg:<22} {stats['covered']:>4}/{stats['statements']:<4} ({stats['coveragePct']:.2f}%)")
    print("\nLowest coverage files:")
    for item in sorted(file_reports, key=lambda x: (x["coveragePct"], -x["missed"]))[:10]:
        if item["statements"] > 0:
            print(f"  {item['file']:<48} {item['covered']:>4}/{item['statements']:<4} ({item['coveragePct']:.2f}%) missed={item['uncoveredSampleLines'][:10]}")
    print(f"Saved: {OUTPUT_PATH}")
    return int(exit_code)


if __name__ == "__main__":
    raise SystemExit(main())
