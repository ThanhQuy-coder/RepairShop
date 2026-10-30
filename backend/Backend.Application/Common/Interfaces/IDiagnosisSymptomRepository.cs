using RepairShop.Domain.Common.Enums;
using RepairShop.Domain.Modules.Diagnostics;

namespace RepairShop.Application.Common.Interfaces;

public interface IDiagnosisSymptomRepository
{
    Task<List<DiagnosisSymptom>> ListAsync(DeviceType? deviceType, bool? isActive);
    Task<DiagnosisSymptom?> GetByIdAsync(Guid id);
    Task<List<DiagnosisSymptom>> GetByIdsAsync(IEnumerable<Guid> ids);
    Task AddAsync(DiagnosisSymptom symptom);
    void Update(DiagnosisSymptom symptom);
    Task SaveChangesAsync();
}
