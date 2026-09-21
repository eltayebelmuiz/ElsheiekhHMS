namespace ElsheiekhHMS.Application.Common.Auditing;

public interface IAuditEventWriter
{
    Task RecordAsync(
        AuditEventRequest request,
        CancellationToken cancellationToken = default);
}
