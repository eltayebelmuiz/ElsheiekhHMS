using ElsheiekhHMS.Core.Domain.Scheduling.Enums;

namespace ElsheiekhHMS.Application.Queue.Contracts;

public sealed record QueueEntryDetailsDto(
    int Id,
    string QueueNumber,
    int PatientId,
    int DepartmentId,
    int? AppointmentId,
    int? DoctorId,
    DateOnly QueueDate,
    QueuePriority Priority,
    QueueStatus Status,
    DateTimeOffset RegisteredAt,
    string? Notes,
    DateTimeOffset? CalledAt,
    DateTimeOffset? CompletedAt,
    string? ConcurrencyToken);
