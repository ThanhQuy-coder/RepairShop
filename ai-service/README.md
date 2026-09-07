# AI Advisory Service — Ghi chú vận hành

## Network isolation (bắt buộc khi deploy)
Service này CHỈ được gọi bởi ASP.NET Core Backend (server-to-server qua X-Internal-Api-Key),
KHÔNG được expose ra internet công khai hay đứng sau public API Gateway.

Khi deploy bằng Docker Compose (Tuần 8), đặt ai-service trong network nội bộ riêng,
KHÔNG map port ra ngoài host (không có `ports:` publish 8000 ra ngoài) — chỉ Backend
container mới truy cập được qua tên service nội bộ (VD: http://ai-service:8000).

## Environment variables bắt buộc (không hard-code)
- INTERNAL_API_KEY: chuỗi bí mật ngẫu nhiên, PHẢI khớp với giá trị Backend dùng để gọi.
- LLM_API_KEY: API key của LLM Provider.