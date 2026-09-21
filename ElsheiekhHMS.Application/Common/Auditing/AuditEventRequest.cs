namespace ElsheiekhHMS.Application.Common.Auditing;

public sealed record AuditEventRequest(
    string Category,
    string Action,
    string? TargetType = null,
    string? TargetId = null,
    string? Reason = null,
    IReadOnlyDictionary<string, string?>? Metadata = null);
