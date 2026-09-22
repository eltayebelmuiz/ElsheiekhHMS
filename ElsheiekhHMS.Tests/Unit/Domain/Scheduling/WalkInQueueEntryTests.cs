using ElsheiekhHMS.Core.Domain.Scheduling.Entities;
using ElsheiekhHMS.Core.Domain.Scheduling.Enums;
using ElsheiekhHMS.Core.Exceptions;
using ElsheiekhHMS.Core.Interfaces;

namespace ElsheiekhHMS.Tests.Unit.Domain.Scheduling;

public class WalkInQueueEntryTests
{
    private static readonly DateOnly QueueDate = new(2026, 9, 21);
    private static readonly DateTimeOffset RegisteredAt =
        new(2026, 9, 21, 7, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset HandoffAt =
        new(2026, 9, 21, 7, 15, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset CompletedAt =
        new(2026, 9, 21, 7, 45, 0, TimeSpan.Zero);

    [Fact]
    public void Construction_preserves_patient_department_ticket_and_audit_data()
    {
        var entry = CreateEntry(notes: "  registration routing  ");

        Assert.Equal(41, entry.PatientId);
        Assert.Equal(3, entry.DepartmentId);
        Assert.Equal(QueueDate, entry.QueueDate);
        Assert.Equal(7, entry.SequenceNumber);
        Assert.Equal("A-007", entry.QueueNumber);
        Assert.Equal(QueuePriority.Normal, entry.Priority);
        Assert.Equal(QueueStatus.Waiting, entry.Status);
        Assert.Equal("registration routing", entry.Notes);
        Assert.Equal(RegisteredAt, entry.RegisteredAt);
        Assert.Equal(RegisteredAt, entry.CreatedAt);
        Assert.Equal("desk-1", entry.CreatedBy);
        Assert.Null(entry.DoctorId);
        Assert.Null(entry.CalledAt);
        Assert.Null(entry.CompletedAt);
        Assert.False(entry.IsDeleted);
        Assert.IsAssignableFrom<IHasConcurrencyToken>(entry);
    }

    [Fact]
    public void Ordinary_walk_in_entry_has_no_appointment_link()
    {
        Assert.Null(CreateEntry().AppointmentId);
    }

    [Fact]
    public void Appointment_linked_entry_preserves_appointment_id()
    {
        var entry = CreateEntry(appointmentId: 19);

        Assert.Equal(19, entry.AppointmentId);
    }

    [Theory]
    [InlineData(0, 3)]
    [InlineData(-1, 3)]
    [InlineData(41, 0)]
    [InlineData(41, -1)]
    public void Required_ids_must_be_positive(int patientId, int departmentId)
    {
        Assert.Throws<DomainValidationException>(() => CreateEntry(patientId, departmentId));
    }

    [Theory]
    [InlineData(0, "A-000")]
    [InlineData(1000, "A-000")]
    [InlineData(7, "A-008")]
    [InlineData(7, "a-007")]
    [InlineData(7, "A-07")]
    [InlineData(7, "A-٠٠٧")]
    public void Queue_ticket_requires_exact_shape_and_sequence_agreement(
        int sequenceNumber, string queueNumber)
    {
        Assert.Throws<DomainValidationException>(() =>
            CreateEntry(sequenceNumber: sequenceNumber, queueNumber: queueNumber));
    }

    [Fact]
    public void Default_date_and_undefined_priority_are_rejected()
    {
        Assert.Throws<DomainValidationException>(() => CreateEntry(queueDate: DateOnly.MinValue));
        Assert.Throws<DomainValidationException>(() =>
            CreateEntry(priority: (QueuePriority)100));
    }

    [Theory]
    [InlineData(QueuePriority.Normal)]
    [InlineData(QueuePriority.Urgent)]
    [InlineData(QueuePriority.Emergency)]
    public void Every_approved_queue_priority_can_be_stored(QueuePriority priority)
    {
        var entry = CreateEntry(priority: priority);

        Assert.Equal(priority, entry.Priority);
    }

    [Fact]
    public void Non_utc_registration_time_is_rejected()
    {
        Assert.Throws<DomainValidationException>(() =>
            CreateEntry(registeredAt: new DateTimeOffset(2026, 9, 21, 7, 0, 0,
                TimeSpan.FromHours(2))));
    }

    [Fact]
    public void Waiting_entry_can_follow_nurse_then_doctor_to_queue_completion()
    {
        var entry = CreateEntry();
        entry.CallToNurse(RegisteredAt, "nurse-1");
        entry.SendToDoctor(17, HandoffAt, "desk-2");
        entry.CompleteQueue(CompletedAt, "desk-2");

        Assert.Equal(QueueStatus.Completed, entry.Status);
        Assert.Equal(17, entry.DoctorId);
        Assert.Equal(RegisteredAt, entry.CalledAt);
        Assert.Equal(CompletedAt, entry.CompletedAt);
        Assert.Equal(CompletedAt, entry.UpdatedAt);
        Assert.Equal("desk-2", entry.UpdatedBy);
        Assert.False(entry.IsDeleted);
    }

    [Fact]
    public void Waiting_entry_can_skip_nurse_and_go_directly_to_doctor()
    {
        var entry = CreateEntry();
        entry.SendToDoctor(17, HandoffAt, "desk-2");

        Assert.Equal(QueueStatus.AtDoctor, entry.Status);
        Assert.Equal(17, entry.DoctorId);
        Assert.Equal(HandoffAt, entry.CalledAt);
    }

    [Fact]
    public void Hold_and_resume_return_entry_to_waiting_without_erasing_first_call()
    {
        var entry = CreateEntry();
        entry.CallToNurse(RegisteredAt, "nurse-1");
        Assert.Throws<BusinessRuleException>(() => entry.Hold(HandoffAt, "nurse-1"));

        var waiting = CreateEntry();
        waiting.Hold(HandoffAt, "desk-1");
        waiting.Resume(CompletedAt, "desk-1");

        Assert.Equal(QueueStatus.Waiting, waiting.Status);
        Assert.Null(waiting.CalledAt);
        Assert.Null(waiting.CompletedAt);
    }

    [Theory]
    [InlineData(QueueStatus.Waiting)]
    [InlineData(QueueStatus.AtNurse)]
    [InlineData(QueueStatus.AtDoctor)]
    [InlineData(QueueStatus.OnHold)]
    public void Any_active_state_can_be_cancelled_without_soft_deletion(QueueStatus state)
    {
        var entry = CreateEntry();
        switch (state)
        {
            case QueueStatus.AtNurse:
                entry.CallToNurse(RegisteredAt, "nurse-1");
                break;
            case QueueStatus.AtDoctor:
                entry.SendToDoctor(17, HandoffAt, "desk-1");
                break;
            case QueueStatus.OnHold:
                entry.Hold(HandoffAt, "desk-1");
                break;
        }

        entry.Cancel(CompletedAt, "desk-1");

        Assert.Equal(QueueStatus.Cancelled, entry.Status);
        Assert.False(entry.IsDeleted);
        Assert.Equal(CompletedAt, entry.UpdatedAt);
    }

    [Fact]
    public void Invalid_or_terminal_transitions_do_not_change_state()
    {
        var entry = CreateEntry();
        Assert.Throws<BusinessRuleException>(() => entry.CompleteQueue(CompletedAt, "desk-1"));
        Assert.Equal(QueueStatus.Waiting, entry.Status);

        entry.SendToDoctor(17, HandoffAt, "desk-1");
        entry.CompleteQueue(CompletedAt, "desk-1");
        Assert.Throws<BusinessRuleException>(() => entry.Cancel(CompletedAt, "desk-2"));
        Assert.Throws<BusinessRuleException>(() => entry.Resume(CompletedAt, "desk-2"));
        Assert.Equal(QueueStatus.Completed, entry.Status);
        Assert.Equal(CompletedAt, entry.CompletedAt);
    }

    [Fact]
    public void Non_utc_action_time_is_rejected_before_mutation()
    {
        var entry = CreateEntry();
        var localTime = new DateTimeOffset(2026, 9, 21, 7, 0, 0, TimeSpan.FromHours(2));

        Assert.Throws<DomainValidationException>(() => entry.Hold(localTime, "desk-1"));
        Assert.Equal(QueueStatus.Waiting, entry.Status);
        Assert.Null(entry.UpdatedAt);
    }

    private static WalkInQueueEntry CreateEntry(
        int patientId = 41,
        int departmentId = 3,
        DateOnly? queueDate = null,
        int sequenceNumber = 7,
        string queueNumber = "A-007",
        QueuePriority priority = QueuePriority.Normal,
        string? notes = null,
        DateTimeOffset? registeredAt = null,
        int? appointmentId = null) =>
        new(patientId, departmentId, queueDate ?? QueueDate, sequenceNumber,
            queueNumber, priority, notes, registeredAt ?? RegisteredAt, "desk-1", appointmentId);
}
