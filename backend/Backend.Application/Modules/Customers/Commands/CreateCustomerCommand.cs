using RepairShop.Application.Modules.Customers.DTOs;
using MediatR;

namespace RepairShop.Application.Modules.Customers.Commands;

public record CreateCustomerCommand(string FullName, string Phone, string? Email, string? Address)
    : IRequest<CustomerResponse>;