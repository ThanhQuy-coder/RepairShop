# Kết quả đo môi trường AI Service (Task 1.3)

- Thời điểm đo: 2026-11-09T12:01:35+00:00
- Python: 3.12.0 — Windows-11-10.0.26300-SP0
- CPU logic: 12
- LLM mode: hosted / model: gemini-3.5-flash-lite

## Tài nguyên

| Hạng mục | Giá trị |
|---|---|
| RSS tiến trình bench | 39.2 MB |
| RSS sau khi import app.main + chạy advisory | 52.8 MB |
| RAM hệ thống (total / available) | 7918.8 / 700.2 MB |
| GPU | NVIDIA GeForce RTX 3050 Laptop GPU |
| VRAM total / used / free | 4096.0 / 0.0 / 4096.0 MB |

## Cold start (tiến trình mới)

| Hạng mục | ms |
|---|---|
| import app.main | 1145.6 |
| import langgraph.graph | 2330.5 |
| import sklearn | 2868.9 |
| compile StateGraph tối giản | 0.7 |

## Độ trễ (LLM mock, không gọi mạng)

| Luồng | n | median | p95 | min | max |
|---|---|---|---|---|---|
| AdvisoryService (in-process) | 20 | 0.2 | 0.4 | 0.2 | 0.4 |
| POST /v1/advisory (TestClient) | 20 | 6.4 | 10.5 | 5.0 | 50.1 |

- `GET /health`: 4.9 ms (HTTP 200)
- `GET /health/deep`: 16.2 ms (HTTP 200)

## Ghi chú

- LLM được mock (FakeLLMClient) nên số đo phản ánh hạ tầng, KHÔNG phải chất lượng sinh văn bản.
- Cold start đo bằng subprocess Python mới; lần chạy thứ hai sẽ nhanh hơn do OS cache.
- Đo 20 lần sau 3 lần warm-up; p95 tính bằng nội suy tuyến tính.
- VRAM lấy từ nvidia-smi tại thời điểm đo, không phải VRAM riêng của tiến trình AI Service.
