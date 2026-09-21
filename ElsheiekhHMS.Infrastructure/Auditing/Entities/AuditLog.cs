namespace ElsheiekhHMS.Infrastructure.Auditing.Entities;

public sealed class AuditLog
{
    private AuditLog()
    {
    }

    public long Id { get; private set; }
    public DateTimeOffset OccurredAtUtc { get; private set; }
    public string ActorKind { get; private set; } = null!;
    public string? ActorUserId { get; private set; }
    public string? ActorUserNameSnapshot { get; private set; }
    public string Category { get; private set; } = null!;
    public string Action { get; private set; } = null!;
    public string? TargetType { get; private set; }
    public string? TargetId { get; private set; }
    public string? Reason { get; private set; }
    public string? MetadataJson { get; private set; }
    public string? CorrelationId { get; private set; }

    internal static AuditLog Create(
        DateTimeOffset occurredAtUtc,
        string actorKind,
        string? actorUserId,
        string? actorUserNameSnapshot,
        string category,
        string action,
        string? targetType,
        string? targetId,
        string? reason,
        string? metadataJson,
        string? correlationId)
    {
        return new AuditLog
        {
            OccurredAtUtc = occurredAtUtc,
            ActorKind = actorKind,
            ActorUserId = actorUserId,
            ActorUserNameSnapshot = actorUserNameSnapshot,
            Category = category,
            Action = action,
            TargetType = targetType,
            TargetId = targetId,
            Reason = reason,
            MetadataJson = metadataJson,
            CorrelationId = correlationId
        };
    }
}
