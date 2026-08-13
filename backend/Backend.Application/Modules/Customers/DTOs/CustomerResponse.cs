namespace Backend.Application.Modules.Customers.DTOs;

public record CustomerResponse(Guid Id, string FullName, string Phone, string? Email, string? Address, DateTime CreatedAt);

public record CustomerListResponse(List<CustomerResponse> Items, int Total);