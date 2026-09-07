namespace RepairShop.Application.Modules.AIAdvisory.DTOs;

// Application chỉ định nghĩa "cần gì" — hoàn toàn không biết FastAPI, HTTP, hay contract JSON thật sự
public record AIAdviceRequest(string DeviceType, string Brand, string Model, string IssueDescription);