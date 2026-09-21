using ElsheiekhHMS.Core.Domain.Scheduling.Enums;

namespace ElsheiekhHMS.Application.Appointments.Contracts;

public sealed record AppointmentDetailsDto(
    int Id,
    string AppointmentCode,
    int PatientId,
    int DoctorId,
    int DepartmentId,
    DateOnly ScheduledDate,
    TimeOnly ScheduledTime,
    AppointmentType Type,
    AppointmentStatus Status,
    string? Notes,
    string? CancellationReason,
    DateTimeOffset? CancelledAt,
    string? ConcurrencyToken);
