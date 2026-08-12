# 🛠️ Electronic Device Repair & Warranty Management System (Hệ Thống Quản Lý Sửa Chữa & Bảo Hành Thiết Bị)

> **Đồ án Thực tập Tốt nghiệp**  
> **Tác giả:** Huỳnh Thanh Quý  
> **Kiến trúc Backend:** Clean Architecture / CQRS pattern với MediatR, FluentValidation & Policy-Based Authorization.

---

## 📖 1. Tổng Quan Đề Tài (Overview)

Hệ thống quản lý tập trung trên nền tảng Web dành cho các cửa hàng sửa chữa điện thoại, laptop và thiết bị điện tử[cite: 1]. Hệ thống giúp số hóa toàn bộ quy trình nghiệp vụ từ **Tiếp nhận ➔ Báo giá ➔ Sửa chữa ➔ Quản lý kho linh kiện ➔ Bảo hành & Bàn giao**, tích hợp **AI Service** hỗ trợ tư vấn giá và linh kiện[cite: 1].

### 🌟 Key Features (Chức năng chính)
* **Phân quyền & Tài khoản (Identity):** Quản lý người dùng với 4 Roles chính: `Admin`, `Receptionist`, `Technician`, `Customer`[cite: 1].
* **Quy trình Sửa chữa (Workflow):** Theo dõi tiến độ thời gian thực qua các trạng thái: *Tiếp nhận ➔ Kiểm tra ➔ Báo giá ➔ Chờ xác nhận ➔ Sửa chữa ➔ Kiểm thử ➔ Hoàn thành ➔ Bàn giao*[cite: 1].
* **Quản lý Kho & Vật tư:** Nhập/xuất linh kiện, tự động trừ tồn kho khi sử dụng cho phiếu sửa chữa[cite: 1].
* **AI Smart Assistant:** Tích hợp AI Service (LLM/RAG/Function Calling) giúp tự động tư vấn dịch vụ, dự đoán linh kiện thay thế và khoảng giá dựa trên dữ liệu cửa hàng[cite: 1].
* **Tra cứu công khai:** Khách hàng tra cứu tiến độ sửa chữa bằng mã phiếu mà không cần đăng nhập[cite: 1].
* **Dashboard & Thống kê:** Báo cáo doanh thu, lượng phiếu sửa chữa, dịch vụ phổ biến và linh kiện tồn kho[cite: 1].

---

## 🛠️ 2. Công Nghệ Sử Dụng (Tech Stack)

### Backend (Core Services)
* **Framework:** ASP.NET Core Web API (.NET 10)[cite: 1]
* **Architecture:** Clean Architecture + CQRS Pattern (MediatR)
* **Database & ORM:** PostgreSQL + Entity Framework Core[cite: 1]
* **Security & Auth:** JWT Bearer Token, Policy-Based Authorization, ASP.NET Core Identity
* **Validation & Exceptions:** FluentValidation (Pipeline Behavior), Global Exception Handling Middleware

### AI Service
* **Framework:** FastAPI (Python 3.11+)[cite: 1]
* **AI Integration:** OpenAI / Local LLMs (RAG / Function Calling)[cite: 1]

### Frontend
* **Framework:** React + Vite[cite: 1]
* **State & UI:** Axios, React Router, TailwindCSS / Ant Design

### Infrastructure & DevOps
* **Storage:** Cloudinary (Lưu ảnh thiết bị, bài viết)[cite: 1]
* **Containerization:** Docker, Docker Compose[cite: 1]

---

## 🏗️ 3. Kiến Trúc Backend (Backend Architecture)

Hệ thống áp dụng **Clean Architecture** kết hợp mô hình **CQRS** giúp tách biệt việc Đọc (Query) và Ghi (Command), đảm bảo tính mở rộng, bảo trì và dễ dàng viết Unit Test:

```text
Backend/
├── Backend.Domain/           # Core Enterprise Logic (Entities, Value Objects, Domain Exceptions, Enums/Roles)
├── Backend.Application/      # Business Logic (MediatR Commands/Queries, DTOs, FluentValidation, Pipeline Behaviors)
├── Backend.Infrastructure/   # External Concerns (EF Core DbContext, Migrations, JWT Generator, Repositories)
├── Backend.Shared/           # Common Models (ApiErrorResponse, PagedList, Cross-cutting DTOs)
└── Backend.API/              # Entry Point (Controllers, Middlewares, Program.cs, Configuration)
```

⚡ Highlights Highlights trong implementation:
Validation Pipeline: Tự động validate dữ liệu đầu vào qua ValidationBehaviour của MediatR trước khi chạy Handler [Validation Error -> 400 Bad Request].

Global Exception Handling: Catch và chuẩn hóa toàn bộ Exception tầng ứng dụng về format ApiErrorResponse thống nhất.

Policy-Based Authorization: Quản lý quyền truy cập linh hoạt bằng các Policy như AdminOnly, StaffOnly, InventoryViewers thay vì hardcode string rải rác.

🚀 4. Hướng Dẫn Khởi Chạy (Getting Started)
Yêu cầu hệ thống (Prerequisites)
.NET 10 SDK

Node.js (v18+) & npm / pnpm

Python 3.11+

PostgreSQL (v15+) hoặc Docker Desktop

Running via Docker Compose (Fastest)
Bash


# Clone repository
git clone [https://github.com/ThanhQuy-coder/RepairShop.git](https://github.com/ThanhQuy-coder/RepairShop.git)
cd repair-management-system

# Run entire stack (Backend, Frontend, AI Service, Postgres)
docker-compose up -d --build
📑 5. Tài liệu API & Postman
Swagger UI: http://localhost:5000/swagger (Sau khi khởi chạy Backend)
