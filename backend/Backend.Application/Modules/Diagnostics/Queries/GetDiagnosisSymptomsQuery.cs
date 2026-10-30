using MediatR;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.Diagnostics.DTOs;
using RepairShop.Domain.Common.Enums;

namespace RepairShop.Application.Modules.Diagnostics.Queries;

public record GetDiagnosisSymptomsQuery(DeviceType? DeviceType, bool? IsActive)
    : IRequest<DiagnosisSymptomListResponse>;

public class GetDiagnosisSymptomsQueryHandler
    : IRequestHandler<GetDiagnosisSymptomsQuery, DiagnosisSymptomListResponse>
{
    private readonly IDiagnosisSymptomRepository _repository;

    public GetDiagnosisSymptomsQueryHandler(IDiagnosisSymptomRepository repository) => _repository = repository;

    public async Task<DiagnosisSymptomListResponse> Handle(
        GetDiagnosisSymptomsQuery request, CancellationToken cancellationToken)
    {
        var items = await _repository.ListAsync(request.DeviceType, request.IsActive);
        return new DiagnosisSymptomListResponse(items.Select(ToResponse).ToList());
    }

    internal static DiagnosisSymptomResponse ToResponse(RepairShop.Domain.Modules.Diagnostics.DiagnosisSymptom symptom) =>
        new(symptom.Id, symptom.Name, symptom.DeviceType.ToString(), symptom.IsActive, symptom.SortOrder);
}
