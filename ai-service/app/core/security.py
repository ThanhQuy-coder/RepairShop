from fastapi import Header, HTTPException, status # type: ignore

from app.core.config import settings


def verify_internal_api_key(x_internal_api_key: str = Header(...)) -> None:
    """
    Xác thực request đến từ Backend (ASP.NET Core), không phải từ Frontend hay bên ngoài.
    Đúng thiết kế Task 5 Tuần 2: 'Auth: Service-to-service (API key nội bộ, không dùng JWT người dùng)'.
    """
    if x_internal_api_key != settings.internal_api_key:
        raise HTTPException(
            status_code=status.HTTP_401_UNAUTHORIZED,
            detail="Invalid or missing internal API key.",
        )