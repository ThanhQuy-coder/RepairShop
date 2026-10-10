using MediatR;
using RepairShop.Application.Modules.SLA.DTOs;

namespace RepairShop.Application.Modules.SLA.Queries;

public sealed record GetSlaPoliciesQuery : IRequest<IReadOnlyList<SlaPolicyResponse>>;
