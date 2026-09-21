using ElsheiekhHMS.Core.Domain.Scheduling.Enums;

namespace ElsheiekhHMS.Application.Appointments.Contracts;

public sealed record CreateAppointmentRequest(
    int PatientId,
    int DoctorId,
    int DepartmentId,
    DateOnly ScheduledDate,
    TimeOnly ScheduledTime,
    AppointmentType Type,
    string? Notes)
{
    public string? Notes { get; } = Optional(Notes);

    private static string? Optional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
