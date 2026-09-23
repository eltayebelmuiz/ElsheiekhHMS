using ElsheiekhHMS.Core.Common;
using ElsheiekhHMS.Core.Domain.Clinical.Enums;
using ElsheiekhHMS.Core.Exceptions;
using ElsheiekhHMS.Core.Interfaces;

namespace ElsheiekhHMS.Core.Domain.Clinical.Entities;

public sealed class Encounter : AuditableEntity, IHasConcurrencyToken
{
    public Encounter(
        int patientId,
        int departmentId,
        int doctorId,
        int queueEntryId,
        DateTimeOffset startedAt,
        string? createdBy)
    {
        ValidateId(patientId, "Patient ID");
        ValidateId(departmentId, "Department ID");
        ValidateId(doctorId, "Doctor ID");
        ValidateId(queueEntryId, "Queue entry ID");
        EnsureUtc(startedAt, "Start time");

        PatientId = patientId;
        DepartmentId = departmentId;
        DoctorId = doctorId;
        QueueEntryId = queueEntryId;
        Status = EncounterStatus.InProgress;
        StartedAt = startedAt;
        CreatedAt = startedAt;
        CreatedBy = createdBy;
    }

    public int PatientId { get; }

    public int DepartmentId { get; }

    public int DoctorId { get; }

    public int QueueEntryId { get; }

    public EncounterStatus Status { get; private set; }

    public DateTimeOffset StartedAt { get; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public byte[] RowVersion { get; set; } = [];

    public void Complete(DateTimeOffset completedAt, string? updatedBy)
    {
        if (Status != EncounterStatus.InProgress)
        {
            throw new BusinessRuleException(
                "Only an in-progress encounter can be completed.");
        }

        EnsureUtc(completedAt, "Completion time");
        if (completedAt < StartedAt)
        {
            throw new DomainValidationException(
                "Completion time cannot precede encounter start.");
        }

        Status = EncounterStatus.Completed;
        CompletedAt = completedAt;
        UpdatedAt = completedAt;
        UpdatedBy = updatedBy;
    }

    private static void ValidateId(int value, string label)
    {
        if (value <= 0)
        {
            throw new DomainValidationException($"{label} must be positive.");
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
