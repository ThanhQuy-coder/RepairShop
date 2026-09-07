from fastapi import FastAPI # type: ignore

from app.api import advisory
from app.core.config import settings

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


app.include_router(advisory.router, prefix="/v1", tags=["Advisory"])