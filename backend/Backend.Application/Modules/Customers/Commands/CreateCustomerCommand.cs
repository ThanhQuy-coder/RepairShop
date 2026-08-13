using Backend.Application.Modules.Customers.DTOs;
using MediatR;

namespace Backend.Application.Modules.Customers.Commands;

public record CreateCustomerCommand(string FullName, string Phone, string? Email, string? Address)
    : IRequest<CustomerResponse>;