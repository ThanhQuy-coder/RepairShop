#!/bin/bash
dotnet run --project backend/Backend.API &
cd frontend && npm run dev