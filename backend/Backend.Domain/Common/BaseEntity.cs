namespace RepairShop.Domain.Common;

/// <summary>
/// Base entity cho toàn bộ Aggregate Root có Guid Id + Audit fields.
/// KHÔNG áp dụng cho lookup entity thuần (Role, RepairStatus) — 2 bảng đó
/// dùng int Id và không có audit theo đúng Data Dictionary Tuần 2.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();

    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; protected set; }

    public DateTime? DeletedAt { get; protected set; }

    public bool IsDeleted => DeletedAt.HasValue;

    public void MarkUpdated() => UpdatedAt = DateTime.UtcNow;

    public void MarkDeleted() => DeletedAt = DateTime.UtcNow;

    public void Restore() => DeletedAt = null;
}