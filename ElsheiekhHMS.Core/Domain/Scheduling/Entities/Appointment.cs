using ElsheiekhHMS.Core.Common;
using ElsheiekhHMS.Core.Domain.Scheduling.Enums;
using ElsheiekhHMS.Core.Exceptions;
using ElsheiekhHMS.Core.Interfaces;

namespace ElsheiekhHMS.Core.Domain.Scheduling.Entities;

public sealed class Appointment : SoftDeletableEntity, IHasConcurrencyToken
{
    public Appointment(
        string appointmentCode,
        int patientId,
        int doctorId,
        int departmentId,
        DateOnly scheduledDate,
        TimeOnly scheduledTime,
        AppointmentType type,
        string? notes,
        DateTimeOffset createdAt,
        string? createdBy)
    {
        AppointmentCode = Required(appointmentCode, "Appointment code");
        ValidateId(patientId, "Patient ID");
        ValidateId(doctorId, "Doctor ID");
        ValidateId(departmentId, "Department ID");
        ValidateDate(scheduledDate);
        ValidateEnum(type, "Appointment type");
        EnsureUtc(createdAt, "Creation time");

        PatientId = patientId;
        DoctorId = doctorId;
        DepartmentId = departmentId;
        ScheduledDate = scheduledDate;
        ScheduledTime = scheduledTime;
        Type = type;
        Notes = Optional(notes);
        Status = AppointmentStatus.Scheduled;
        CreatedAt = createdAt;
        CreatedBy = createdBy;
    }

    public string AppointmentCode { get; }

    public int PatientId { get; }

    public int DoctorId { get; }

    public int DepartmentId { get; }

    public DateOnly ScheduledDate { get; }

    public TimeOnly ScheduledTime { get; }

    public AppointmentType Type { get; }

    public AppointmentStatus Status { get; private set; }

    public string? Notes { get; }

    public string? CancellationReason { get; private set; }

    public DateTimeOffset? CancelledAt { get; private set; }

    public byte[] RowVersion { get; set; } = [];

    public void Confirm(DateTimeOffset updatedAt, string? updatedBy)
    {
        EnsureTransition(AppointmentStatus.Scheduled, "Only a scheduled appointment can be confirmed.");
        SetStatus(AppointmentStatus.Confirmed, updatedAt, updatedBy);
    }

    public void CheckIn(DateTimeOffset updatedAt, string? updatedBy)
    {
        EnsureTransition(
            status => status is AppointmentStatus.Scheduled or AppointmentStatus.Confirmed,
            "Only a scheduled or confirmed appointment can be checked in.");
        SetStatus(AppointmentStatus.CheckedIn, updatedAt, updatedBy);
    }

    public void Complete(DateTimeOffset updatedAt, string? updatedBy)
    {
        EnsureTransition(AppointmentStatus.CheckedIn, "Only a checked-in appointment can be completed.");
        SetStatus(AppointmentStatus.Completed, updatedAt, updatedBy);
    }

    public void Cancel(string? reason, DateTimeOffset cancelledAt, string? cancelledBy)
    {
        EnsureTransition(
            status => status is AppointmentStatus.Scheduled or AppointmentStatus.Confirmed,
            "Only a scheduled or confirmed appointment can be cancelled.");
        EnsureUtc(cancelledAt, "Cancellation time");

        Status = AppointmentStatus.Cancelled;
        CancellationReason = Optional(reason);
        CancelledAt = cancelledAt;
        UpdatedAt = cancelledAt;
        UpdatedBy = cancelledBy;
    }

    public void MarkNoShow(DateTimeOffset updatedAt, string? updatedBy)
    {
        EnsureTransition(
            status => status is AppointmentStatus.Scheduled or AppointmentStatus.Confirmed,
            "Only a scheduled or confirmed appointment can be marked as no-show.");
        SetStatus(AppointmentStatus.NoShow, updatedAt, updatedBy);
    }

    private void SetStatus(
        AppointmentStatus status,
        DateTimeOffset updatedAt,
        string? updatedBy)
    {
        EnsureUtc(updatedAt, "Update time");
        Status = status;
        UpdatedAt = updatedAt;
        UpdatedBy = updatedBy;
    }

    private void EnsureTransition(
        AppointmentStatus requiredStatus,
        string message) =>
        EnsureTransition(status => status == requiredStatus, message);

    private void EnsureTransition(
        Func<AppointmentStatus, bool> allowed,
        string message)
    {
        if (IsDeleted)
        {
            throw new BusinessRuleException("Deleted appointments cannot be changed.");
        }

        if (!allowed(Status))
        {
            throw new BusinessRuleException(message);
        }
    }

    private static string Required(string? value, string label) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new DomainValidationException($"{label} is required.")
            : value.Trim();

    private static string? Optional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void ValidateId(int value, string label)
    {
        if (value <= 0)
        {
            throw new DomainValidationException($"{label} must be positive.");
        }
    }

    private static void ValidateDate(DateOnly value)
    {
        if (value == DateOnly.MinValue)
        {
            throw new DomainValidationException("Scheduled date is required.");
        }
    }

    private static void ValidateEnum<T>(T value, string label) where T : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw new DomainValidationException($"{label} is invalid.");
        }
    }

    private static void EnsureUtc(DateTimeOffset value, string label)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new DomainValidationException($"{label} must be UTC.");
        }
    }
}
