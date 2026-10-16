#!/bin/bash

# 1. Clean project backend và chờ hoàn thành
echo "🧹 Cleaning backend..."
dotnet clean backend/Backend.API

# 2. Khởi chạy backend dưới nền
echo "🚀 Starting backend..."
dotnet run --project backend/Backend.API &

# 3. Chuyển sang frontend và khởi chạy
echo "💻 Starting frontend..."
cd frontend && npm run dev