namespace ElsheiekhHMS.Core.Common;

public abstract class SoftDeletableEntity : AuditableEntity
{
    // Derived domain behavior must update this metadata together, using UTC.
    public bool IsDeleted { get; protected set; }
    public DateTimeOffset? DeletedAt { get; protected set; }
    public string? DeletedBy { get; protected set; }
}
