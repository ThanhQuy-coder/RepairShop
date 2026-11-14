# RepairShop

Hệ thống quản lý sửa chữa, linh kiện và bảo hành thiết bị điện tử, được xây dựng
cho cửa hàng sửa chữa điện thoại, laptop và các thiết bị điện tử khác.

> Đồ án Thực tập Tốt nghiệp  
> Tác giả: **Huỳnh Thanh Quý**

## Tổng quan

RepairShop số hóa quy trình từ lúc tiếp nhận thiết bị đến khi bàn giao:

```text
Tiếp nhận -> Kiểm tra -> Báo giá -> Chờ xác nhận
-> Sửa chữa -> Kiểm thử -> Hoàn thành -> Bàn giao/Bảo hành
```

Hệ thống gồm ba thành phần chính:

- **Backend**: ASP.NET Core Web API, xử lý nghiệp vụ, xác thực, phân quyền,
  database và các API cho frontend.
- **Frontend**: React + TypeScript + Vite, cung cấp giao diện cho nhân viên,
  kỹ thuật viên và khách hàng.
- **AI Service**: FastAPI/Python, cung cấp tư vấn chẩn đoán, báo giá, đặt lịch,
  bảo hành và tra cứu tiến độ thông qua Multi-Agent System.

## Tính năng chính

- Quản lý tài khoản và phân quyền theo vai trò.
- Tiếp nhận và theo dõi phiếu sửa chữa theo từng trạng thái.
- Quản lý dịch vụ, linh kiện, tồn kho và giá tham khảo.
- Tư vấn AI dựa trên context thật do Backend cung cấp.
- Gợi ý nguyên nhân, dịch vụ và linh kiện phù hợp với thiết bị.
- Lập báo giá, không cho AI tự tạo hoặc tự thay đổi giá.
- Gợi ý khung giờ đặt lịch, không tự xác nhận lịch hẹn.
- Quản lý và tra cứu bảo hành.
- Tra cứu công khai tiến độ sửa chữa bằng mã phiếu.
- Dashboard và báo cáo nghiệp vụ.

## Kiến trúc hệ thống

```text
                         +----------------------+
                         |      React/Vite      |
                         |      Frontend        |
                         +----------+-----------+
                                    |
                                    | HTTP
                                    v
+-------------------+     +--------+---------+     +------------------+
| PostgreSQL        |<--->| ASP.NET Core API |---->| FastAPI AI       |
| Database          |     | Backend          |     | Service          |
+-------------------+     +------------------+     +--------+---------+
                                                           |
                                                           v
                                                     Ollama local
```

Backend gọi AI Service theo mô hình server-to-server bằng `X-Internal-Api-Key`.
AI Service không truy cập PostgreSQL trực tiếp; Backend gửi dữ liệu nghiệp vụ
cần thiết trong request context.

### Cấu trúc repository

```text
RepairShop/
├── backend/
│   ├── Backend.API/             # HTTP entry point, controllers, middleware
│   ├── Backend.Application/     # CQRS, MediatR, handlers, validators
│   ├── Backend.Domain/          # Entities, enums, domain rules
│   ├── Backend.Infrastructure/  # EF Core, PostgreSQL, migrations, integrations
│   └── Backend.Shared/          # Shared response and cross-cutting models
├── frontend/                    # React, TypeScript, Vite
├── ai-service/                  # FastAPI, MAS, retrieval, model artifacts
├── tests/
│   ├── Backend.UnitTests/
│   └── Backend.IntegrationTests/
├── local-docs/                  # Tài liệu kỹ thuật, báo cáo và hướng dẫn cục bộ
├── RepairShop.slnx
└── start.sh                     # Khởi động Backend và Frontend trên môi trường Unix
```

## Công nghệ sử dụng

### Backend

- .NET 10 / ASP.NET Core Web API
- Clean Architecture
- CQRS với MediatR
- Entity Framework Core và PostgreSQL
- ASP.NET Core Identity, JWT Bearer và Policy-based Authorization
- FluentValidation
- Serilog
- Swagger/OpenAPI

### Frontend

- React 19
- TypeScript
- Vite
- React Router
- Axios
- Zustand
- SignalR
- Framer Motion

### AI Service

- Python 3.12
- FastAPI và Uvicorn
- Pydantic/Pydantic Settings
- LangGraph
- scikit-learn và joblib
- Ollama local qua OpenAI-compatible Chat Completions API

## Yêu cầu môi trường

- .NET 10 SDK
- Node.js và npm
- Python 3.12
- PostgreSQL hoặc môi trường database tương ứng của Backend
- Ollama local nếu muốn chạy AI ở chế độ LLM

Kiểm tra nhanh:

```powershell
dotnet --version
node --version
npm --version
py --version
```

## Khởi chạy Backend

Từ thư mục root:

```powershell
dotnet restore RepairShop.slnx
dotnet build RepairShop.slnx
dotnet run --project backend\Backend.API\Backend.API.csproj
```

Các thiết lập database, JWT và tích hợp ngoài cần được cấu hình theo
`appsettings.Development.json` hoặc User Secrets của Backend. Không commit
connection string, API key hay JWT secret vào Git.

Sau khi chạy, mở Swagger tại URL được in trong terminal, thường có dạng:

```text
http://localhost:<backend-port>/swagger
```

## Khởi chạy Frontend

```powershell
cd frontend
npm install
npm run dev
```

Các lệnh hữu ích:

```powershell
npm run build
npm run lint
npm run preview
```

## Khởi chạy AI Service

```powershell
cd ai-service
py -3.12 -m venv venv
.\venv\Scripts\python.exe -m pip install -r requirements.txt
.\venv\Scripts\python.exe -m pip install -r requirements-mas.txt
Copy-Item .env.example .env
```

Trong `.env`, tối thiểu cần đặt:

```dotenv
INTERNAL_API_KEY=replace-with-a-private-key
LLM_MODE=offline
```

`LLM_MODE=offline` phù hợp để kiểm thử logic deterministic mà không cần Ollama.
Để chạy đầy đủ với LLM local, dùng:

```dotenv
LLM_MODE=ollama
LLM_BASE_URL=http://127.0.0.1:11434/v1
LLM_API_KEY=ollama
LLM_MODEL=qwen2.5:1.5b-instruct-q4_K_M
```

Khởi động service:

```powershell
.\venv\Scripts\python.exe -m uvicorn app.main:app --host 127.0.0.1 --port 8000
```

Kiểm tra:

```powershell
curl.exe http://127.0.0.1:8000/health
curl.exe http://127.0.0.1:8000/health/deep
```

Swagger của AI Service:

```text
http://127.0.0.1:8000/docs
```

### Chạy Ollama local

Khi sử dụng `LLM_MODE=ollama`, cần bảo đảm Ollama đang chạy và model đã được tải:

```powershell
ollama pull qwen2.5:1.5b-instruct-q4_K_M
ollama serve
```

AI Service chỉ cho phép `LLM_BASE_URL` trỏ tới host local được cấu hình trong
`app/core/config.py`; không cấu hình URL provider cloud trong môi trường này.

## Kiểm thử

### Backend

```powershell
dotnet test RepairShop.slnx
```

Hoặc chạy từng project:

```powershell
dotnet test tests\Backend.UnitTests\Backend.UnitTests.csproj
dotnet test tests\Backend.IntegrationTests\Backend.IntegrationTests.csproj
```

### Frontend

```powershell
cd frontend
npm run lint
npm run build
```

### AI Service

```powershell
cd ai-service
.\venv\Scripts\python.exe -m pytest tests -q
```

Một số nhóm test quan trọng:

```powershell
.\venv\Scripts\python.exe -m pytest tests\test_pipeline.py tests\test_graph.py tests\test_routing.py -q
.\venv\Scripts\python.exe -m pytest tests\test_api_agents.py tests\test_security.py tests\test_schemas.py -q
.\venv\Scripts\python.exe -m pytest tests\test_business_rules.py tests\test_security_and_resilience.py -q
```

## AI Service: nguyên tắc dữ liệu quan trọng

AI Service được thiết kế theo hướng **context-first** và **zero-trust**:

1. Backend là nguồn dữ liệu về giá, tồn kho, lịch trống và trạng thái phiếu.
2. AI chỉ được chọn ID tồn tại trong context của request.
3. Giá từ LLM bị ghi đè bằng giá Backend trước khi trả response.
4. Slot đầy hoặc không thuộc context bị loại.
5. Đặt lịch chỉ tạo `bookingDraft`; Backend chỉ tạo lịch sau khi khách xác nhận.
6. Chẩn đoán là nguyên nhân khả dĩ, không thay thế kết luận của kỹ thuật viên.
7. Request có thể chạy offline bằng rule/retrieval hoặc dùng Ollama local.

Các endpoint chính:

| Method | Endpoint | Mục đích |
|---|---|---|
| `GET` | `/health` | Kiểm tra process |
| `GET` | `/health/deep` | Kiểm tra dependency và model |
| `GET` | `/v1/advisory/ping` | Kiểm tra legacy advisory router |
| `POST` | `/v1/advisory` | Tư vấn legacy |
| `POST` | `/v1/agents/execute` | Chạy Multi-Agent System |
| `POST` | `/v1/baseline/execute` | Chạy Single-Agent baseline |

Chi tiết luồng, schema, train model và cách test AI được trình bày tại:

- [Giải thích toàn bộ mã nguồn AI Service](local-docs/ai-service-giai-thich-ma-nguon.md)
- [Hướng dẫn chạy và kiểm thử AI Service](local-docs/ai-service-huong-dan-chay-va-test.md)

## Tài liệu dự án

- [Tài liệu AI Service](local-docs/ai-service-giai-thich-ma-nguon.md)
- [Hướng dẫn chạy và test AI Service](local-docs/ai-service-huong-dan-chay-va-test.md)
- [Tài liệu kỹ thuật AI](local-docs/feature-ai/)
- [Báo cáo và tài liệu môn học](local-docs/report/)
- [Tài liệu ngữ cảnh dự án](local-docs/context/CONTEXT.md)
- [Frontend README](frontend/README.md)

## Bảo mật và triển khai

- Không commit `.env`, credentials, connection string hoặc model/API secret.
- AI Service chỉ nên nằm trong private network; không publish trực tiếp ra Internet.
- Backend phải xác thực `X-Internal-Api-Key` khi gọi AI Service.
- Cấu hình production cần tách secret khỏi source code bằng secret manager hoặc
  biến môi trường của hệ thống triển khai.
- Trước khi deploy, chạy build và test cho cả Backend, Frontend và AI Service.

## Trạng thái phát triển

Repository hiện được tổ chức thành solution .NET, ứng dụng React/Vite và AI
Service độc lập. Các lệnh chạy trong README này ưu tiên môi trường phát triển
local; cấu hình production, reverse proxy, database migration và secret management
cần được thiết lập riêng theo hạ tầng triển khai thực tế.
