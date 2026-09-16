namespace ElsheiekhHMS.Core.Common;

public abstract class AuditableEntity : BaseEntity
{
    // Supply UTC timestamps and opaque user identifiers at the application boundary.
    public DateTimeOffset CreatedAt { get; protected set; }
    public string? CreatedBy { get; protected set; }
    public DateTimeOffset? UpdatedAt { get; protected set; }
    public string? UpdatedBy { get; protected set; }
}
