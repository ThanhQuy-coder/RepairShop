using RepairShop.Application.Modules.Customers.DTOs;
using MediatR;

namespace RepairShop.Application.Modules.Customers.Queries;

public record GetCustomersQuery(string? Search, int Page = 1, int PageSize = 20) : IRequest<CustomerListResponse>;