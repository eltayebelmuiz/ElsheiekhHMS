using ElsheiekhHMS.Core.Domain.Clinical.Entities;
using ElsheiekhHMS.Core.Domain.Clinical.Enums;
using ElsheiekhHMS.Core.Domain.Organization.Entities;
using ElsheiekhHMS.Core.Domain.Patients.Entities;
using ElsheiekhHMS.Core.Domain.Patients.Enums;
using ElsheiekhHMS.Core.Domain.Scheduling.Entities;
using ElsheiekhHMS.Core.Domain.Scheduling.Enums;
using ElsheiekhHMS.Core.Domain.Staff.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ElsheiekhHMS.Tests.Integration.Persistence;

[Collection("SQL Server persistence")]
public sealed class EncounterSqlServerTests(SqlServerTestDatabaseFixture fixture)
{
    private static int _sequence;

    [Fact]
    public async Task Encounter_roundtrips_all_persisted_values_and_rowversion()
    {
        var graph = await SeedGraphAsync();
        var startedAt = Utc(1);
        var encounter = CreateEncounter(graph, startedAt);

        await using (var context = fixture.CreateContext())
        {
            context.Encounters.Add(encounter);
            await context.SaveChangesAsync();
        }

        await using var readContext = fixture.CreateContext();
        var reloaded = await readContext.Encounters.SingleAsync(item => item.Id == encounter.Id);
        Assert.Equal(encounter.Id, reloaded.Id);
        Assert.Equal(graph.Patient.Id, reloaded.PatientId);
        Assert.Equal(graph.Department.Id, reloaded.DepartmentId);
        Assert.Equal(graph.Doctor.Id, reloaded.DoctorId);
        Assert.Equal(graph.Queue.Id, reloaded.QueueEntryId);
        Assert.Equal(EncounterStatus.InProgress, reloaded.Status);
        Assert.Equal(startedAt, reloaded.StartedAt);
        Assert.Null(reloaded.CompletedAt);
        Assert.NotEmpty(reloaded.RowVersion);
    }

    [Fact]
    public async Task In_progress_encounter_keeps_completed_at_null()
    {
        var graph = await SeedGraphAsync();
        var encounter = CreateEncounter(graph, Utc(2));

        await using var context = fixture.CreateContext();
        context.Encounters.Add(encounter);
        await context.SaveChangesAsync();

        var persisted = await context.Encounters.SingleAsync(item => item.Id == encounter.Id);
        Assert.Equal(EncounterStatus.InProgress, persisted.Status);
        Assert.Null(persisted.CompletedAt);
    }

    [Fact]
    public async Task Completed_encounter_persists_domain_completion()
    {
        var graph = await SeedGraphAsync();
        var startedAt = Utc(3);
        var completedAt = Utc(4);
        var encounter = CreateEncounter(graph, startedAt);
        encounter.Complete(completedAt, "integration-editor");

        await using var context = fixture.CreateContext();
        context.Encounters.Add(encounter);
        await context.SaveChangesAsync();

        var persisted = await context.Encounters.SingleAsync(item => item.Id == encounter.Id);
        Assert.Equal(EncounterStatus.Completed, persisted.Status);
        Assert.Equal(completedAt, persisted.CompletedAt);
        Assert.Equal(completedAt, persisted.UpdatedAt);
        Assert.Equal("integration-editor", persisted.UpdatedBy);
    }

    [Fact]
    public async Task Encounter_rowversion_rejects_a_stale_update()
    {
        var graph = await SeedGraphAsync();
        var encounter = CreateEncounter(graph, Utc(5));
        await using (var seed = fixture.CreateContext())
        {
            seed.Encounters.Add(encounter);
            await seed.SaveChangesAsync();
        }

        await using var first = fixture.CreateContext();
        await using var second = fixture.CreateContext();
        var firstEncounter = await first.Encounters.SingleAsync(item => item.Id == encounter.Id);
        var secondEncounter = await second.Encounters.SingleAsync(item => item.Id == encounter.Id);
        firstEncounter.Complete(Utc(6), "first");
        await first.SaveChangesAsync();
        secondEncounter.Complete(Utc(7), "second");

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());
    }

    [Fact]
    public async Task Duplicate_queue_entry_is_rejected_by_the_unique_database_invariant()
    {
        var graph = await SeedGraphAsync();
        await using (var first = fixture.CreateContext())
        {
            first.Encounters.Add(CreateEncounter(graph, Utc(8)));
            await first.SaveChangesAsync();
        }

        await using var second = fixture.CreateContext();
        second.Encounters.Add(CreateEncounter(graph, Utc(9)));
        await Assert.ThrowsAsync<DbUpdateException>(() => second.SaveChangesAsync());
    }

    [Fact]
    public async Task Invalid_patient_foreign_key_is_rejected()
    {
        var graph = await SeedGraphAsync();
        await Assert.ThrowsAsync<DbUpdateException>(() => SaveEncounterAsync(
            new Encounter(999991, graph.Department.Id, graph.Doctor.Id, graph.Queue.Id, Utc(10), "integration")));
    }

    [Fact]
    public async Task Invalid_department_foreign_key_is_rejected()
    {
        var graph = await SeedGraphAsync();
        await Assert.ThrowsAsync<DbUpdateException>(() => SaveEncounterAsync(
            new Encounter(graph.Patient.Id, 999992, graph.Doctor.Id, graph.Queue.Id, Utc(11), "integration")));
    }

    [Fact]
    public async Task Invalid_doctor_foreign_key_is_rejected()
    {
        var graph = await SeedGraphAsync();
        await Assert.ThrowsAsync<DbUpdateException>(() => SaveEncounterAsync(
            new Encounter(graph.Patient.Id, graph.Department.Id, 999993, graph.Queue.Id, Utc(12), "integration")));
    }

    [Fact]
    public async Task Invalid_queue_foreign_key_is_rejected()
    {
        var graph = await SeedGraphAsync();
        await Assert.ThrowsAsync<DbUpdateException>(() => SaveEncounterAsync(
            new Encounter(graph.Patient.Id, graph.Department.Id, graph.Doctor.Id, 999994, Utc(13), "integration")));
    }

    [Fact]
    public async Task Referenced_patient_cannot_be_deleted()
    {
        var graph = await SeedGraphWithEncounterAsync(14);
        await using var context = fixture.CreateContext();
        context.Patients.Remove(await context.Patients.SingleAsync(item => item.Id == graph.Patient.Id));
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task Referenced_department_cannot_be_deleted()
    {
        var graph = await SeedGraphWithEncounterAsync(15);
        await using var context = fixture.CreateContext();
        context.Departments.Remove(await context.Departments.SingleAsync(item => item.Id == graph.Department.Id));
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task Referenced_doctor_cannot_be_deleted()
    {
        var graph = await SeedGraphWithEncounterAsync(16);
        await using var context = fixture.CreateContext();
        context.Doctors.Remove(await context.Doctors.SingleAsync(item => item.Id == graph.Doctor.Id));
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task Referenced_queue_entry_cannot_be_deleted()
    {
        var graph = await SeedGraphWithEncounterAsync(17);
        await using var context = fixture.CreateContext();
        context.WalkInQueueEntries.Remove(
            await context.WalkInQueueEntries.SingleAsync(item => item.Id == graph.Queue.Id));
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task Encounter_table_has_no_appointment_identity_or_soft_delete_columns()
    {
        await using var context = fixture.CreateContext();
        var forbiddenColumns = await context.Database.SqlQueryRaw<int>(
                """
                SELECT COUNT(*) AS [Value]
                FROM sys.columns
                WHERE object_id = OBJECT_ID(N'dbo.Encounters')
                  AND name IN ('AppointmentId', 'ApplicationUserId', 'ProviderUserId', 'IdentityUserId', 'IsDeleted', 'DeletedAt')
                """)
            .SingleAsync();

        Assert.Equal(0, forbiddenColumns);
    }

    [Fact]
    public async Task Encounter_queries_support_bounded_clinical_registry_shape()
    {
        var graph = await SeedGraphWithEncounterAsync(18);
        await using var context = fixture.CreateContext();

        var result = await context.Encounters
            .Where(item => item.PatientId == graph.Patient.Id)
            .Where(item => item.DoctorId == graph.Doctor.Id)
            .Where(item => item.DepartmentId == graph.Department.Id)
            .Where(item => item.Status == EncounterStatus.InProgress)
            .Where(item => item.StartedAt >= Utc(18).AddMinutes(-1))
            .OrderByDescending(item => item.StartedAt)
            .Select(item => new { item.Id, item.PatientId, item.DoctorId, item.Status, item.StartedAt })
            .Take(10)
            .SingleAsync();

        Assert.Equal(graph.Patient.Id, result.PatientId);
        Assert.Equal(graph.Doctor.Id, result.DoctorId);
    }

    private async Task<EncounterGraph> SeedGraphWithEncounterAsync(int second)
    {
        var graph = await SeedGraphAsync();
        var encounter = CreateEncounter(graph, Utc(second));
        await using var context = fixture.CreateContext();
        context.Encounters.Add(encounter);
        await context.SaveChangesAsync();
        return graph;
    }

    private async Task SaveEncounterAsync(Encounter encounter)
    {
        await using var context = fixture.CreateContext();
        context.Encounters.Add(encounter);
        await context.SaveChangesAsync();
    }

    private async Task<EncounterGraph> SeedGraphAsync()
    {
        var department = new Department($"Encounter {Next()}", null, null, Utc(20), "integration");
        var patient = new Patient(
            $"PT-2099-{Next() % 89999 + 1:D5}", "Encounter", "Test", null, "Patient",
            new DateOnly(1990, 1, 1), Gender.Female, BloodGroup.OPositive,
            null, null, $"0999{Next() % 999999:D6}", "Integration address", "Kigali",
            null, null, null, null, new DateOnly(2026, 12, 31), Utc(21), "integration");

        await using (var context = fixture.CreateContext())
        {
            context.Departments.Add(department);
            context.Patients.Add(patient);
            await context.SaveChangesAsync();
        }

        var doctor = new Doctor(
            $"ENC-{Next():D6}", "Encounter Doctor", "Internal Medicine", false,
            100m, department.Id, Utc(22), "integration");
        await using (var context = fixture.CreateContext())
        {
            context.Doctors.Add(doctor);
            await context.SaveChangesAsync();
        }

        var sequence = Next() % 999 + 1;
        var queue = new WalkInQueueEntry(
            patient.Id, department.Id, new DateOnly(2026, 12, 1), sequence,
            $"A-{sequence:D3}", QueuePriority.Normal, "integration queue",
            Utc(23), "integration");
        await using (var context = fixture.CreateContext())
        {
            context.WalkInQueueEntries.Add(queue);
            await context.SaveChangesAsync();
        }

        return new EncounterGraph(department, patient, doctor, queue);
    }

    private static Encounter CreateEncounter(EncounterGraph graph, DateTimeOffset startedAt) =>
        new(graph.Patient.Id, graph.Department.Id, graph.Doctor.Id, graph.Queue.Id, startedAt, "integration");

    private static int Next() => 10000 + Interlocked.Increment(ref _sequence);

    private static DateTimeOffset Utc(int second) =>
        new(2026, 1, 1, 12, 0, second, TimeSpan.Zero);

    private sealed record EncounterGraph(
        Department Department,
        Patient Patient,
        Doctor Doctor,
        WalkInQueueEntry Queue);
}
