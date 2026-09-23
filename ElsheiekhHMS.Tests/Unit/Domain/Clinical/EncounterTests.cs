using ElsheiekhHMS.Core.Domain.Clinical.Entities;
using ElsheiekhHMS.Core.Domain.Clinical.Enums;
using ElsheiekhHMS.Core.Exceptions;
using Xunit;

namespace ElsheiekhHMS.Tests.Unit.Domain.Clinical;

public class EncounterTests
{
    private static readonly DateTimeOffset StartedAt =
        new(2026, 9, 23, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Constructor_creates_in_progress_encounter_with_immutable_provenance()
    {
        var encounter = CreateEncounter();

        Assert.Equal(EncounterStatus.InProgress, encounter.Status);
        Assert.Equal(StartedAt, encounter.StartedAt);
        Assert.Null(encounter.CompletedAt);
        Assert.Equal(17, encounter.PatientId);
        Assert.Equal(4, encounter.DepartmentId);
        Assert.Equal(9, encounter.DoctorId);
        Assert.Equal(23, encounter.QueueEntryId);
        Assert.Equal(StartedAt, encounter.CreatedAt);
        Assert.Equal("user-1", encounter.CreatedBy);
        Assert.Null(encounter.UpdatedAt);
        Assert.Null(encounter.UpdatedBy);
        Assert.Empty(encounter.RowVersion);
    }

    [Fact]
    public void Complete_transitions_to_completed_and_records_completion_metadata()
    {
        var encounter = CreateEncounter();
        var completedAt = StartedAt.AddMinutes(45);

        encounter.Complete(completedAt, "user-2");

        Assert.Equal(EncounterStatus.Completed, encounter.Status);
        Assert.Equal(completedAt, encounter.CompletedAt);
        Assert.Equal(completedAt, encounter.UpdatedAt);
        Assert.Equal("user-2", encounter.UpdatedBy);
    }

    [Fact]
    public void Complete_rejects_a_time_before_start_without_mutating_encounter()
    {
        var encounter = CreateEncounter();

        Assert.Throws<DomainValidationException>(() =>
            encounter.Complete(StartedAt.AddTicks(-1), "user-2"));

        Assert.Equal(EncounterStatus.InProgress, encounter.Status);
        Assert.Null(encounter.CompletedAt);
        Assert.Null(encounter.UpdatedAt);
        Assert.Null(encounter.UpdatedBy);
    }

    [Fact]
    public void Complete_rejects_a_second_completion_without_replacing_metadata()
    {
        var encounter = CreateEncounter();
        var firstCompletion = StartedAt.AddMinutes(45);
        encounter.Complete(firstCompletion, "user-2");

        Assert.Throws<BusinessRuleException>(() =>
            encounter.Complete(firstCompletion.AddMinutes(5), "user-3"));

        Assert.Equal(EncounterStatus.Completed, encounter.Status);
        Assert.Equal(firstCompletion, encounter.CompletedAt);
        Assert.Equal(firstCompletion, encounter.UpdatedAt);
        Assert.Equal("user-2", encounter.UpdatedBy);
    }

    [Fact]
    public void Constructor_rejects_non_utc_start_time()
    {
        Assert.Throws<DomainValidationException>(() =>
            new Encounter(17, 4, 9, 23, new DateTimeOffset(2026, 9, 23, 10, 0, 0,
                TimeSpan.FromHours(2)), "user-1"));
    }

    [Fact]
    public void Complete_rejects_non_utc_completion_time_without_mutating_encounter()
    {
        var encounter = CreateEncounter();

        Assert.Throws<DomainValidationException>(() =>
            encounter.Complete(new DateTimeOffset(2026, 9, 23, 12, 0, 0,
                TimeSpan.FromHours(2)), "user-2"));

        Assert.Equal(EncounterStatus.InProgress, encounter.Status);
        Assert.Null(encounter.CompletedAt);
    }

    [Theory]
    [InlineData(0, 4, 9, 23)]
    [InlineData(17, 0, 9, 23)]
    [InlineData(17, 4, 0, 23)]
    [InlineData(17, 4, 9, 0)]
    public void Constructor_rejects_nonpositive_provenance_ids(
        int patientId, int departmentId, int doctorId, int queueEntryId)
    {
        Assert.Throws<DomainValidationException>(() =>
            new Encounter(patientId, departmentId, doctorId, queueEntryId, StartedAt, "user-1"));
    }

    private static Encounter CreateEncounter() =>
        new(17, 4, 9, 23, StartedAt, "user-1");
}
