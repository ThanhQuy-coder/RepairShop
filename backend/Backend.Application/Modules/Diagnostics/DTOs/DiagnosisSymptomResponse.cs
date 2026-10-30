namespace RepairShop.Application.Modules.Diagnostics.DTOs;

public record DiagnosisSymptomResponse(Guid Id, string Name, string DeviceType, bool IsActive, int SortOrder);
public record DiagnosisSymptomListResponse(List<DiagnosisSymptomResponse> Items);
