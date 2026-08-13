using Backend.Application.Modules.Customers.DTOs;
using MediatR;

namespace Backend.Application.Modules.Customers.Queries;

public record GetCustomerByIdQuery(Guid Id) : IRequest<CustomerResponse>;