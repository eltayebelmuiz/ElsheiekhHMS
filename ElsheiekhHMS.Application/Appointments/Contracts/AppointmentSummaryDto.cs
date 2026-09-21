using ElsheiekhHMS.Core.Domain.Scheduling.Enums;

namespace ElsheiekhHMS.Application.Appointments.Contracts;

public sealed record AppointmentSummaryDto(
    int Id,
    string AppointmentCode,
    int PatientId,
    int DoctorId,
    int DepartmentId,
    DateOnly ScheduledDate,
    TimeOnly ScheduledTime,
    AppointmentType Type,
    AppointmentStatus Status);
