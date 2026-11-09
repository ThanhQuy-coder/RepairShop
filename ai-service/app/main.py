from fastapi import FastAPI # type: ignore

from app.api import advisory, agents, baseline
from app.core.config import settings
from app.core.health import build_health_report

app = FastAPI(
    title="RepairShop AI Advisory Service",
    description=(
        "Dịch vụ AI tư vấn giá dịch vụ/linh kiện dựa trên dữ liệu cửa hàng. "
        "Chỉ phục vụ ASP.NET Core Backend gọi nội bộ, KHÔNG public ra ngoài, "
        "KHÔNG kết nối trực tiếp PostgreSQL (đúng phạm vi đã khóa ở Task 6 Tuần 1)."
    ),
    version="1.0.0",
)


@app.get("/health", tags=["Health"])
def health_check():
    """
    Mục đích: ASP.NET Core Backend gọi endpoint này để biết AI Service có đang hoạt động
    hay không TRƯỚC KHI gửi request advisory thật — tránh timeout vô ích nếu service đã down.
    """
    return {"status": "healthy"}


@app.get("/health/deep", tags=["Health"])
def health_check_deep(probe: bool = False):
    """
    Task 1.3 — health check chi tiết cho lớp MAS: LangGraph, scikit-learn, model intent, LLM.
    Mặc định không gọi mạng; `?probe=true` mới kiểm tra kết nối LLM provider (cache 30s).
    Luôn trả HTTP 200 khi tiến trình còn phục vụ — trạng thái nằm trong field `status`.
    """
    return build_health_report(probe=probe)


app.include_router(advisory.router, prefix="/v1", tags=["Advisory"])
app.include_router(agents.router, prefix="/v1", tags=["Agents"])
app.include_router(baseline.router, prefix="/v1", tags=["Baseline"])