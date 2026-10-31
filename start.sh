#!/bin/bash

# dotnet ef migrations add InitialCreate --project backend/Backend.Infrastructure --startup-project backend/Backend.API

# dotnet ef database update --project backend/Backend.Infrastructure --startup-project backend/Backend.API

# 2. Khởi chạy backend dưới nền
echo "🚀 Starting backend..."
dotnet run --project backend/Backend.API &

# 3. Chuyển sang frontend và khởi chạy
echo "💻 Starting frontend..."
cd frontend && npm run dev