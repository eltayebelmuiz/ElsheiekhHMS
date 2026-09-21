using ElsheiekhHMS.Core.Common;
using ElsheiekhHMS.Core.Domain.Scheduling.Enums;
using ElsheiekhHMS.Core.Exceptions;
using ElsheiekhHMS.Core.Interfaces;

namespace ElsheiekhHMS.Core.Domain.Scheduling.Entities;

public sealed class WalkInQueueEntry : SoftDeletableEntity, IHasConcurrencyToken
{
    public WalkInQueueEntry(
        int patientId,
        int departmentId,
        DateOnly queueDate,
        int sequenceNumber,
        string queueNumber,
        QueuePriority priority,
        string? notes,
        DateTimeOffset registeredAt,
        string? createdBy)
    {
        ValidateId(patientId, "Patient ID");
        ValidateId(departmentId, "Department ID");
        ValidateDate(queueDate);
        ValidateTicket(sequenceNumber, queueNumber);
        ValidateEnum(priority, "Queue priority");
        EnsureUtc(registeredAt, "Registration time");

        PatientId = patientId;
        DepartmentId = departmentId;
        QueueDate = queueDate;
        SequenceNumber = sequenceNumber;
        QueueNumber = queueNumber;
        Priority = priority;
        Notes = Optional(notes);
        Status = QueueStatus.Waiting;
        RegisteredAt = registeredAt;
        CreatedAt = registeredAt;
        CreatedBy = createdBy;
    }

    public int PatientId { get; }

    public int DepartmentId { get; }

    public int? DoctorId { get; private set; }

    public DateOnly QueueDate { get; }

    public int SequenceNumber { get; }

    public string QueueNumber { get; }

    public QueuePriority Priority { get; }

    public QueueStatus Status { get; private set; }

    public string? Notes { get; }

    public DateTimeOffset RegisteredAt { get; }

    public DateTimeOffset? CalledAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public byte[] RowVersion { get; set; } = [];

    public void CallToNurse(DateTimeOffset calledAt, string? updatedBy)
    {
        EnsureTransition(QueueStatus.Waiting, "Only a waiting entry can be called to the nurse.");
        EnsureUtc(calledAt, "Call time");

        Status = QueueStatus.AtNurse;
        CalledAt ??= calledAt;
        UpdatedAt = calledAt;
        UpdatedBy = updatedBy;
    }

    public void SendToDoctor(int doctorId, DateTimeOffset calledAt, string? updatedBy)
    {
        EnsureTransition(
            status => status is QueueStatus.Waiting or QueueStatus.AtNurse,
            "Only a waiting or nurse-stage entry can be sent to a doctor.");
        ValidateId(doctorId, "Doctor ID");
        EnsureUtc(calledAt, "Call time");

        Status = QueueStatus.AtDoctor;
        DoctorId = doctorId;
        CalledAt ??= calledAt;
        UpdatedAt = calledAt;
        UpdatedBy = updatedBy;
    }

    public void Hold(DateTimeOffset updatedAt, string? updatedBy)
    {
        EnsureTransition(QueueStatus.Waiting, "Only a waiting entry can be placed on hold.");
        SetStatus(QueueStatus.OnHold, updatedAt, updatedBy);
    }

    public void Resume(DateTimeOffset updatedAt, string? updatedBy)
    {
        EnsureTransition(QueueStatus.OnHold, "Only an entry on hold can be resumed.");
        SetStatus(QueueStatus.Waiting, updatedAt, updatedBy);
    }

    public void CompleteQueue(DateTimeOffset completedAt, string? updatedBy)
    {
        EnsureTransition(QueueStatus.AtDoctor, "Only a doctor-stage entry can be completed.");
        EnsureUtc(completedAt, "Completion time");

        Status = QueueStatus.Completed;
        CompletedAt = completedAt;
        UpdatedAt = completedAt;
        UpdatedBy = updatedBy;
    }

    public void Cancel(DateTimeOffset updatedAt, string? updatedBy)
    {
        EnsureTransition(
            status => status is QueueStatus.Waiting or QueueStatus.AtNurse or
                QueueStatus.AtDoctor or QueueStatus.OnHold,
            "Only an active queue entry can be cancelled.");
        SetStatus(QueueStatus.Cancelled, updatedAt, updatedBy);
    }

    private void SetStatus(QueueStatus status, DateTimeOffset updatedAt, string? updatedBy)
    {
        EnsureUtc(updatedAt, "Update time");
        Status = status;
        UpdatedAt = updatedAt;
        UpdatedBy = updatedBy;
    }

    private void EnsureTransition(QueueStatus requiredStatus, string message) =>
        EnsureTransition(status => status == requiredStatus, message);

    private void EnsureTransition(Func<QueueStatus, bool> allowed, string message)
    {
        if (IsDeleted)
        {
            throw new BusinessRuleException("Deleted queue entries cannot be changed.");
        }

        if (!allowed(Status))
        {
            throw new BusinessRuleException(message);
        }
    }

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
            throw new DomainValidationException("Queue date is required.");
        }
    }

    private static void ValidateTicket(int sequenceNumber, string? queueNumber)
    {
        if (sequenceNumber is < 1 or > 999 ||
            queueNumber is null ||
            queueNumber.Length != 5 ||
            queueNumber[0] != 'A' ||
            queueNumber[1] != '-' ||
            !queueNumber[2..].All(character => character is >= '0' and <= '9') ||
            queueNumber != $"A-{sequenceNumber:D3}")
        {
            throw new DomainValidationException("Queue ticket must match A-NNN and its sequence.");
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
