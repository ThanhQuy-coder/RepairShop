using MediatR;
using RepairShop.Application.Common.Exceptions;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Application.Modules.Diagnostics.DTOs;
using RepairShop.Domain.Common.Enums;
using RepairShop.Domain.Modules.Diagnostics;

namespace RepairShop.Application.Modules.Diagnostics.Commands;

public record CreateDiagnosisSymptomCommand(string Name, DeviceType DeviceType, int SortOrder)
    : IRequest<DiagnosisSymptomResponse>;
public record UpdateDiagnosisSymptomCommand(Guid Id, string Name, DeviceType DeviceType, int SortOrder)
    : IRequest<DiagnosisSymptomResponse>;
public record ToggleDiagnosisSymptomCommand(Guid Id, bool IsActive) : IRequest;

public class CreateDiagnosisSymptomCommandHandler : IRequestHandler<CreateDiagnosisSymptomCommand, DiagnosisSymptomResponse>
{
    private readonly IDiagnosisSymptomRepository _repository;
    public CreateDiagnosisSymptomCommandHandler(IDiagnosisSymptomRepository repository) => _repository = repository;

    public async Task<DiagnosisSymptomResponse> Handle(CreateDiagnosisSymptomCommand request, CancellationToken cancellationToken)
    {
        var symptom = new DiagnosisSymptom(request.Name, request.DeviceType, request.SortOrder);
        await _repository.AddAsync(symptom);
        await _repository.SaveChangesAsync();
        return new(symptom.Id, symptom.Name, symptom.DeviceType.ToString(), symptom.IsActive, symptom.SortOrder);
    }
}

public class UpdateDiagnosisSymptomCommandHandler : IRequestHandler<UpdateDiagnosisSymptomCommand, DiagnosisSymptomResponse>
{
    private readonly IDiagnosisSymptomRepository _repository;
    public UpdateDiagnosisSymptomCommandHandler(IDiagnosisSymptomRepository repository) => _repository = repository;

    public async Task<DiagnosisSymptomResponse> Handle(UpdateDiagnosisSymptomCommand request, CancellationToken cancellationToken)
    {
        var symptom = await _repository.GetByIdAsync(request.Id)
            ?? throw new NotFoundException("Triệu chứng chẩn đoán", request.Id);
        symptom.UpdateInfo(request.Name, request.DeviceType, request.SortOrder);
        _repository.Update(symptom);
        await _repository.SaveChangesAsync();
        return new(symptom.Id, symptom.Name, symptom.DeviceType.ToString(), symptom.IsActive, symptom.SortOrder);
    }
}

public class ToggleDiagnosisSymptomCommandHandler : IRequestHandler<ToggleDiagnosisSymptomCommand>
{
    private readonly IDiagnosisSymptomRepository _repository;
    public ToggleDiagnosisSymptomCommandHandler(IDiagnosisSymptomRepository repository) => _repository = repository;

    public async Task Handle(ToggleDiagnosisSymptomCommand request, CancellationToken cancellationToken)
    {
        var symptom = await _repository.GetByIdAsync(request.Id)
            ?? throw new NotFoundException("Triệu chứng chẩn đoán", request.Id);
        if (request.IsActive) symptom.Activate(); else symptom.Deactivate();
        _repository.Update(symptom);
        await _repository.SaveChangesAsync();
    }
}
