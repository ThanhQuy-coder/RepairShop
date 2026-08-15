using RepairShop.Application.Modules.Customers.DTOs;
using MediatR;

namespace RepairShop.Application.Modules.Customers.Queries;

public record GetCustomerByIdQuery(Guid Id) : IRequest<CustomerResponse>;