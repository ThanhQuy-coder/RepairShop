using Microsoft.EntityFrameworkCore;
using RepairShop.Application.Common.Interfaces;
using RepairShop.Domain.Common.Enums;
using RepairShop.Domain.Modules.Diagnostics;

namespace RepairShop.Infrastructure.Persistence.Repositories;

public class DiagnosisSymptomRepository : IDiagnosisSymptomRepository
{
    private readonly AppDbContext _context;

    public DiagnosisSymptomRepository(AppDbContext context) => _context = context;

    public Task<List<DiagnosisSymptom>> ListAsync(DeviceType? deviceType, bool? isActive)
    {
        var query = _context.Set<DiagnosisSymptom>().AsQueryable();
        if (deviceType is not null) query = query.Where(s => s.DeviceType == deviceType);
        if (isActive is not null) query = query.Where(s => s.IsActive == isActive);
        return query.OrderBy(s => s.DeviceType).ThenBy(s => s.SortOrder).ThenBy(s => s.Name).ToListAsync();
    }

    public Task<DiagnosisSymptom?> GetByIdAsync(Guid id) =>
        _context.Set<DiagnosisSymptom>().FirstOrDefaultAsync(s => s.Id == id);

    public Task<List<DiagnosisSymptom>> GetByIdsAsync(IEnumerable<Guid> ids)
    {
        var idList = ids.Distinct().ToList();
        return _context.Set<DiagnosisSymptom>().Where(s => idList.Contains(s.Id)).ToListAsync();
    }

    public Task AddAsync(DiagnosisSymptom symptom) => _context.Set<DiagnosisSymptom>().AddAsync(symptom).AsTask();

    public void Update(DiagnosisSymptom symptom) => _context.Set<DiagnosisSymptom>().Update(symptom);

    public Task SaveChangesAsync() => _context.SaveChangesAsync();
}
