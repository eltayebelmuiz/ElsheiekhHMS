using ElsheiekhHMS.Core.Common;
using ElsheiekhHMS.Core.Domain.Organization.Entities;
using ElsheiekhHMS.Core.Domain.Patients.Entities;
using ElsheiekhHMS.Core.Domain.Patients.Enums;
using ElsheiekhHMS.Core.Domain.Scheduling.Entities;
using ElsheiekhHMS.Core.Domain.Scheduling.Enums;
using ElsheiekhHMS.Core.Domain.Staff.Entities;
using ElsheiekhHMS.Application.Common.Security;
using Microsoft.EntityFrameworkCore;

namespace ElsheiekhHMS.Tests.Integration.Persistence;

[Collection("SQL Server persistence")]
public sealed class ElsheiekhHmsDbContextSqlServerTests(
    SqlServerTestDatabaseFixture fixture)
{
    private static int _sequence;

    [Fact]
    public async Task InitialCreate_applies_to_the_isolated_database()
    {
        await using var context = fixture.CreateContext();

        Assert.True(await context.Database.CanConnectAsync());
        var applied = await context.Database.GetAppliedMigrationsAsync();
        Assert.Contains("20260921111137_InitialCreate", applied);
        Assert.Contains("20260921182651_AddPhase05IdentityAndAuditLog", applied);
        Assert.Empty(await context.Database.GetPendingMigrationsAsync());
    }

    [Fact]
    public async Task Entity_auditing_uses_stable_user_id_and_utc_time()
    {
        var createdAt = new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
        var currentUser = new TestCurrentUser("user-05c", "display-name", ["Provider"]);
        var patient = CreatePatient();

        await using (var context = fixture.CreateContext(currentUser, new FixedTimeProvider(createdAt)))
        {
            context.Patients.Add(patient);
            await context.SaveChangesAsync();
        }

        await using var readContext = fixture.CreateContext();
        var persisted = await readContext.Patients.SingleAsync(item => item.Id == patient.Id);

        Assert.Equal(createdAt, persisted.CreatedAt);
        Assert.Equal("user-05c", persisted.CreatedBy);
        Assert.Null(persisted.UpdatedAt);
        Assert.Null(persisted.UpdatedBy);
    }

    [Fact]
    public async Task Entity_auditing_preserves_creation_and_stamps_update()
    {
        var createdAt = new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
        var updatedAt = createdAt.AddMinutes(10);
        var patient = CreatePatient();

        await using (var createContext = fixture.CreateContext(
                         new TestCurrentUser("creator", "creator-name", []),
                         new FixedTimeProvider(createdAt)))
        {
            createContext.Patients.Add(patient);
            await createContext.SaveChangesAsync();
        }

        await using (var updateContext = fixture.CreateContext(
                         new TestCurrentUser("editor", "editor-name", []),
                         new FixedTimeProvider(updatedAt)))
        {
            var tracked = await updateContext.Patients.SingleAsync(item => item.Id == patient.Id);
            tracked.UpdateContactDetails(
                "0900111222", "Updated address", "Khartoum", null, null, null, null,
                updatedAt, "spoofed");
            await updateContext.SaveChangesAsync();
        }

        await using var readContext = fixture.CreateContext();
        var persisted = await readContext.Patients.SingleAsync(item => item.Id == patient.Id);

        Assert.Equal(createdAt, persisted.CreatedAt);
        Assert.Equal("creator", persisted.CreatedBy);
        Assert.Equal(updatedAt, persisted.UpdatedAt);
        Assert.Equal("editor", persisted.UpdatedBy);
    }

    [Fact]
    public async Task Entity_auditing_preserves_rowversion_concurrency_failures()
    {
        var patient = CreatePatient();
        await using (var seedContext = fixture.CreateContext())
        {
            seedContext.Patients.Add(patient);
            await seedContext.SaveChangesAsync();
        }

        await using var first = fixture.CreateContext(
            new TestCurrentUser("editor-1", "editor-1", []),
            new FixedTimeProvider(Utc(40)));
        await using var second = fixture.CreateContext(
            new TestCurrentUser("editor-2", "editor-2", []),
            new FixedTimeProvider(Utc(41)));
        var firstPatient = await first.Patients.SingleAsync(item => item.Id == patient.Id);
        var secondPatient = await second.Patients.SingleAsync(item => item.Id == patient.Id);

        firstPatient.UpdateContactDetails(
            "0900222333", "First update", "Khartoum", null, null, null, null,
            Utc(40), "spoofed");
        await first.SaveChangesAsync();
        secondPatient.UpdateContactDetails(
            "0900333444", "Second update", "Khartoum", null, null, null, null,
            Utc(41), "spoofed");

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());
    }

    [Fact]
    public async Task Entity_auditing_keeps_anonymous_actor_null()
    {
        var createdAt = new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
        var patient = CreatePatient();

        await using (var context = fixture.CreateContext(
                         new TestCurrentUser(null, null, [], isAuthenticated: false),
                         new FixedTimeProvider(createdAt)))
        {
            context.Patients.Add(patient);
            await context.SaveChangesAsync();
        }

        await using var readContext = fixture.CreateContext();
        var persisted = await readContext.Patients.SingleAsync(item => item.Id == patient.Id);

        Assert.Null(persisted.CreatedBy);
    }

    [Fact]
    public async Task Entity_auditing_clears_created_soft_delete_metadata()
    {
        var patient = CreatePatient();

        await using (var context = fixture.CreateContext(
                         new TestCurrentUser("creator", "creator", []),
                         new FixedTimeProvider(Utc(45))))
        {
            context.Patients.Add(patient);
            context.Entry(patient).Property(nameof(SoftDeletableEntity.DeletedAt)).CurrentValue = Utc(46);
            context.Entry(patient).Property(nameof(SoftDeletableEntity.DeletedBy)).CurrentValue = "spoofed";
            await context.SaveChangesAsync();
        }

        await using var readContext = fixture.CreateContext();
        var persisted = await readContext.Patients.SingleAsync(item => item.Id == patient.Id);

        Assert.Null(persisted.DeletedAt);
        Assert.Null(persisted.DeletedBy);
    }

    [Fact]
    public async Task Entity_auditing_stamps_explicit_soft_delete_transition()
    {
        var createdAt = Utc(50);
        var deletedAt = Utc(51);
        var patient = CreatePatient();

        await using (var createContext = fixture.CreateContext(
                         new TestCurrentUser("creator", "creator", []),
                         new FixedTimeProvider(createdAt)))
        {
            createContext.Patients.Add(patient);
            await createContext.SaveChangesAsync();
        }

        await using (var deleteContext = fixture.CreateContext(
                         new TestCurrentUser("deleter", "deleter", []),
                         new FixedTimeProvider(deletedAt)))
        {
            var tracked = await deleteContext.Patients.SingleAsync(item => item.Id == patient.Id);
            tracked.MarkDeleted(deletedAt, "spoofed");
            await deleteContext.SaveChangesAsync();
        }

        await using var readContext = fixture.CreateContext();
        var persisted = await readContext.Patients
            .IgnoreQueryFilters()
            .SingleAsync(item => item.Id == patient.Id);

        Assert.True(persisted.IsDeleted);
        Assert.Equal(deletedAt, persisted.DeletedAt);
        Assert.Equal("deleter", persisted.DeletedBy);
        Assert.Equal(deletedAt, persisted.UpdatedAt);
        Assert.Equal("deleter", persisted.UpdatedBy);
    }

    private sealed class TestCurrentUser(
        string? userId,
        string? userName,
        IReadOnlyCollection<string> roles,
        bool isAuthenticated = true) : ICurrentUser
    {
        public bool IsAuthenticated => isAuthenticated;
        public string? UserId { get; } = userId;
        public string? UserName { get; } = userName;
        public IReadOnlyCollection<string> Roles { get; } = roles;
    }

    private sealed class FixedTimeProvider(DateTimeOffset value) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => value;
    }

    [Fact]
    public async Task Patient_roundtrips_through_a_new_context()
    {
        var patient = CreatePatient();

        await using (var writeContext = fixture.CreateContext())
        {
            writeContext.Patients.Add(patient);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = fixture.CreateContext();
        var reloaded = await readContext.Patients.SingleAsync(item => item.Id == patient.Id);

        Assert.Equal(patient.PatientCode, reloaded.PatientCode);
        Assert.Equal(patient.FirstName, reloaded.FirstName);
        Assert.Equal(patient.LastName, reloaded.LastName);
        Assert.Equal(patient.DateOfBirth, reloaded.DateOfBirth);
        Assert.Equal(patient.Gender, reloaded.Gender);
        Assert.Equal(patient.Phone, reloaded.Phone);
        Assert.Equal(patient.Address, reloaded.Address);
    }

    [Fact]
    public async Task Shared_patient_phone_is_allowed()
    {
        var phone = UniquePhone();
        await using var context = fixture.CreateContext();
        context.Patients.Add(CreatePatient(phone: phone));
        context.Patients.Add(CreatePatient(phone: phone));

        await context.SaveChangesAsync();

        Assert.Equal(2, await context.Patients.CountAsync(item => item.Phone == phone));
    }

    [Fact]
    public async Task Department_persists_and_deactivation_is_not_deletion()
    {
        var department = new Department(
            "  Cardiology  ", "Heart care", "101", Utc(1), "integration");

        await using var context = fixture.CreateContext();
        context.Departments.Add(department);
        await context.SaveChangesAsync();

        department.Deactivate(Utc(2), "integration");
        await context.SaveChangesAsync();

        await using var readContext = fixture.CreateContext();
        var reloaded = await readContext.Departments.SingleAsync(item => item.Id == department.Id);
        Assert.Equal("Cardiology", reloaded.Name);
        Assert.False(reloaded.IsActive);
    }

    [Fact]
    public async Task Doctor_schedule_roundtrips_through_the_owned_collection()
    {
        var department = CreateDepartment();
        await using (var seedContext = fixture.CreateContext())
        {
            seedContext.Departments.Add(department);
            await seedContext.SaveChangesAsync();
        }

        var doctor = CreateDoctor(department);
        var schedule = doctor.AddSchedule(
            DayOfWeek.Monday, new TimeOnly(8, 0), new TimeOnly(12, 0), 30,
            Utc(3), "integration");

        await using (var writeContext = fixture.CreateContext())
        {
            writeContext.Doctors.Add(doctor);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = fixture.CreateContext();
        var reloaded = await readContext.Doctors
            .Include(item => item.Schedules)
            .SingleAsync(item => item.Id == doctor.Id);

        var reloadedSchedule = Assert.Single(reloaded.Schedules);
        Assert.Equal(schedule.DayOfWeek, reloadedSchedule.DayOfWeek);
        Assert.Equal(schedule.StartTime, reloadedSchedule.StartTime);
        Assert.Equal(schedule.EndTime, reloadedSchedule.EndTime);
        Assert.Equal(schedule.SlotDurationMinutes, reloadedSchedule.SlotDurationMinutes);
    }

    [Fact]
    public async Task Appointment_persists_relationships_and_scheduling_values()
    {
        var department = CreateDepartment();
        var patient = CreatePatient();
        await using (var context = fixture.CreateContext())
        {
            context.Departments.Add(department);
            context.Patients.Add(patient);
            await context.SaveChangesAsync();
            var doctor = CreateDoctor(department);
            context.Doctors.Add(doctor);
            await context.SaveChangesAsync();
            var appointment = new Appointment(
                UniqueCode("AP"), patient.Id, doctor.Id, department.Id,
                new DateOnly(2026, 10, 15), new TimeOnly(9, 30),
                AppointmentType.Specialist, "follow-up", Utc(4), "integration");
            context.Appointments.Add(appointment);
            await context.SaveChangesAsync();
        }

        await using var readContext = fixture.CreateContext();
        var reloaded = await readContext.Appointments
            .OrderByDescending(item => item.Id)
            .FirstAsync();
        Assert.Equal(patient.Id, reloaded.PatientId);
        Assert.NotEqual(0, reloaded.DoctorId);
        Assert.Equal(department.Id, reloaded.DepartmentId);
        Assert.Equal(new DateOnly(2026, 10, 15), reloaded.ScheduledDate);
        Assert.Equal(new TimeOnly(9, 30), reloaded.ScheduledTime);
        Assert.Equal(AppointmentStatus.Scheduled, reloaded.Status);
    }

    [Fact]
    public async Task Walk_in_queue_entry_persists_patient_and_ticket_values()
    {
        var department = CreateDepartment();
        var patient = CreatePatient();
        int entryId;
        await using (var context = fixture.CreateContext())
        {
            context.Departments.Add(department);
            context.Patients.Add(patient);
            await context.SaveChangesAsync();
            var entry = new WalkInQueueEntry(
                patient.Id, department.Id, new DateOnly(2026, 10, 16), 7, "A-007",
                QueuePriority.Urgent, "requires triage", Utc(5), "integration");
            context.WalkInQueueEntries.Add(entry);
            await context.SaveChangesAsync();
            entryId = entry.Id;
        }

        await using var readContext = fixture.CreateContext();
        var reloaded = await readContext.WalkInQueueEntries
            .SingleAsync(item => item.Id == entryId);
        Assert.Equal(patient.Id, reloaded.PatientId);
        Assert.Equal("A-007", reloaded.QueueNumber);
        Assert.Equal(QueuePriority.Urgent, reloaded.Priority);
        Assert.Equal(new DateOnly(2026, 10, 16), reloaded.QueueDate);
        Assert.Equal(QueueStatus.Waiting, reloaded.Status);
    }

    [Fact]
    public async Task Missing_appointment_principal_is_rejected_by_sql_server()
    {
        var appointment = new Appointment(
            UniqueCode("AP"),
            999991,
            999992,
            999993,
            new DateOnly(2026, 10, 17),
            new TimeOnly(10, 0),
            AppointmentType.General,
            null,
            Utc(6),
            "integration");

        await using var context = fixture.CreateContext();
        context.Appointments.Add(appointment);
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task Deleting_patient_with_history_is_rejected()
    {
        var department = CreateDepartment();
        var patient = CreatePatient();

        await using (var seed = fixture.CreateContext())
        {
            seed.Departments.Add(department);
            seed.Patients.Add(patient);
            await seed.SaveChangesAsync();
            var doctor = CreateDoctor(department);
            seed.Doctors.Add(doctor);
            await seed.SaveChangesAsync();
            seed.Appointments.Add(new Appointment(
                UniqueCode("AP"), patient.Id, doctor.Id, department.Id,
                new DateOnly(2026, 10, 18), new TimeOnly(11, 0),
                AppointmentType.General, null, Utc(7), "integration"));
            await seed.SaveChangesAsync();
        }

        await using var deleteContext = fixture.CreateContext();
        var principal = await deleteContext.Patients.SingleAsync(item => item.Id == patient.Id);
        deleteContext.Remove(principal);
        await Assert.ThrowsAsync<DbUpdateException>(() => deleteContext.SaveChangesAsync());
    }

    [Fact]
    public async Task Deleting_department_with_dependents_is_rejected()
    {
        var department = CreateDepartment();

        await using (var seed = fixture.CreateContext())
        {
            seed.Departments.Add(department);
            await seed.SaveChangesAsync();
            seed.Doctors.Add(CreateDoctor(department));
            await seed.SaveChangesAsync();
        }

        await using var deleteContext = fixture.CreateContext();
        var principal = await deleteContext.Departments.SingleAsync(item => item.Id == department.Id);
        deleteContext.Remove(principal);
        await Assert.ThrowsAsync<DbUpdateException>(() => deleteContext.SaveChangesAsync());
    }

    [Fact]
    public async Task Approved_patient_code_uniqueness_is_enforced()
    {
        var first = CreatePatient();
        var second = CreatePatient(patientCode: first.PatientCode);

        await using var context = fixture.CreateContext();
        context.Patients.Add(first);
        await context.SaveChangesAsync();
        context.Patients.Add(second);

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task Approved_optional_identity_uniqueness_is_enforced()
    {
        var first = CreatePatient(nationalId: "N-1001", passportNumber: "P-1001");
        var second = CreatePatient(nationalId: "N-1001", passportNumber: "P-1002");

        await using var context = fixture.CreateContext();
        context.Patients.Add(first);
        await context.SaveChangesAsync();
        context.Patients.Add(second);

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task Approved_passport_uniqueness_is_enforced()
    {
        var first = CreatePatient(passportNumber: "P-2001");
        var second = CreatePatient(passportNumber: "P-2001");

        await using var context = fixture.CreateContext();
        context.Patients.Add(first);
        await context.SaveChangesAsync();
        context.Patients.Add(second);

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task Duplicate_department_name_is_allowed()
    {
        await using var context = fixture.CreateContext();
        context.Departments.Add(new Department("Shared", null, null, Utc(25), "integration"));
        context.Departments.Add(new Department("Shared", null, null, Utc(26), "integration"));

        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task Queue_ticket_values_are_not_globally_unique()
    {
        var department = CreateDepartment();
        var firstPatient = CreatePatient();
        var secondPatient = CreatePatient();

        await using var context = fixture.CreateContext();
        context.Departments.Add(department);
        context.Patients.AddRange(firstPatient, secondPatient);
        await context.SaveChangesAsync();
        context.WalkInQueueEntries.AddRange(
            CreateQueueEntry(firstPatient.Id, department.Id),
            CreateQueueEntry(secondPatient.Id, department.Id));

        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task Appointment_slot_collisions_are_not_database_unique()
    {
        var department = CreateDepartment();
        var patient = CreatePatient();

        await using var context = fixture.CreateContext();
        context.Departments.Add(department);
        context.Patients.Add(patient);
        await context.SaveChangesAsync();
        var doctor = CreateDoctor(department);
        context.Doctors.Add(doctor);
        await context.SaveChangesAsync();
        context.Appointments.AddRange(
            CreateAppointment(patient.Id, doctor.Id, department.Id),
            CreateAppointment(patient.Id, doctor.Id, department.Id));

        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task Patient_rowversion_rejects_a_stale_update()
    {
        var patient = CreatePatient();
        await using (var seed = fixture.CreateContext())
        {
            seed.Patients.Add(patient);
            await seed.SaveChangesAsync();
        }

        await using var first = fixture.CreateContext();
        await using var second = fixture.CreateContext();
        var firstPatient = await first.Patients.SingleAsync(item => item.Id == patient.Id);
        var secondPatient = await second.Patients.SingleAsync(item => item.Id == patient.Id);
        firstPatient.UpdateContactDetails("0900111222", "First address", null, null, null, null, null, Utc(8), "first");
        await first.SaveChangesAsync();
        secondPatient.UpdateContactDetails("0900333444", "Second address", null, null, null, null, null, Utc(9), "second");

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());
    }

    [Fact]
    public async Task Appointment_rowversion_rejects_a_stale_update()
    {
        var department = CreateDepartment();
        var patient = CreatePatient();
        Appointment appointment;

        await using (var seed = fixture.CreateContext())
        {
            seed.Departments.Add(department);
            seed.Patients.Add(patient);
            await seed.SaveChangesAsync();
            var doctor = CreateDoctor(department);
            seed.Doctors.Add(doctor);
            await seed.SaveChangesAsync();
            appointment = CreateAppointment(patient.Id, doctor.Id, department.Id);
            seed.Appointments.Add(appointment);
            await seed.SaveChangesAsync();
        }

        await using var first = fixture.CreateContext();
        await using var second = fixture.CreateContext();
        var firstAppointment = await first.Appointments.SingleAsync(item => item.Id == appointment.Id);
        var secondAppointment = await second.Appointments.SingleAsync(item => item.Id == appointment.Id);
        firstAppointment.Confirm(Utc(10), "first");
        await first.SaveChangesAsync();
        secondAppointment.CheckIn(Utc(11), "second");

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());
    }

    [Fact]
    public async Task Queue_rowversion_rejects_a_stale_update()
    {
        var department = CreateDepartment();
        var patient = CreatePatient();
        WalkInQueueEntry entry;

        await using (var seed = fixture.CreateContext())
        {
            seed.Departments.Add(department);
            seed.Patients.Add(patient);
            await seed.SaveChangesAsync();
            entry = CreateQueueEntry(patient.Id, department.Id);
            seed.WalkInQueueEntries.Add(entry);
            await seed.SaveChangesAsync();
        }

        await using var first = fixture.CreateContext();
        await using var second = fixture.CreateContext();
        var firstEntry = await first.WalkInQueueEntries.SingleAsync(item => item.Id == entry.Id);
        var secondEntry = await second.WalkInQueueEntries.SingleAsync(item => item.Id == entry.Id);
        firstEntry.Hold(Utc(12), "first");
        await first.SaveChangesAsync();
        secondEntry.Cancel(Utc(13), "second");

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());
    }

    [Fact]
    public async Task Patient_soft_delete_is_filtered_but_persisted()
    {
        var patient = CreatePatient();
        await using (var context = fixture.CreateContext())
        {
            context.Patients.Add(patient);
            await context.SaveChangesAsync();
            patient.MarkDeleted(Utc(14), "integration");
            await context.SaveChangesAsync();
        }

        await using var readContext = fixture.CreateContext();
        Assert.Null(await readContext.Patients.SingleOrDefaultAsync(item => item.Id == patient.Id));
        var persisted = await readContext.Patients
            .IgnoreQueryFilters()
            .SingleAsync(item => item.Id == patient.Id);
        Assert.True(persisted.IsDeleted);
        Assert.Equal("integration", persisted.DeletedBy);
    }

    [Fact]
    public async Task Doctor_soft_delete_metadata_is_persisted()
    {
        var department = CreateDepartment();
        var doctorCode = string.Empty;
        await using (var context = fixture.CreateContext())
        {
            context.Departments.Add(department);
            await context.SaveChangesAsync();
            var doctor = CreateDoctor(department);
            doctorCode = doctor.DoctorCode;
            context.Doctors.Add(doctor);
            await context.SaveChangesAsync();
            doctor.Deactivate(Utc(27), "integration");
            doctor.MarkDeleted(Utc(28), "integration");
            await context.SaveChangesAsync();
        }

        await using var readContext = fixture.CreateContext();
        var persisted = await readContext.Doctors.SingleAsync(item => item.DoctorCode == doctorCode);
        Assert.True(persisted.IsDeleted);
        Assert.Equal("integration", persisted.DeletedBy);
    }

    [Fact]
    public async Task Appointment_soft_delete_is_filtered()
    {
        var department = CreateDepartment();
        var patient = CreatePatient();
        var appointmentId = 0;
        await using (var context = fixture.CreateContext())
        {
            context.Departments.Add(department);
            context.Patients.Add(patient);
            await context.SaveChangesAsync();
            var doctor = CreateDoctor(department);
            context.Doctors.Add(doctor);
            await context.SaveChangesAsync();
            var appointment = CreateAppointment(patient.Id, doctor.Id, department.Id);
            context.Appointments.Add(appointment);
            await context.SaveChangesAsync();
            appointmentId = appointment.Id;
            context.Entry(appointment).Property(nameof(SoftDeletableEntity.IsDeleted)).CurrentValue = true;
            context.Entry(appointment).Property(nameof(SoftDeletableEntity.DeletedAt)).CurrentValue = Utc(29);
            context.Entry(appointment).Property(nameof(SoftDeletableEntity.DeletedBy)).CurrentValue = "integration";
            await context.SaveChangesAsync();
        }

        await using var readContext = fixture.CreateContext();
        Assert.Null(await readContext.Appointments.SingleOrDefaultAsync(item => item.Id == appointmentId));
        var persisted = await readContext.Appointments.IgnoreQueryFilters()
            .SingleAsync(item => item.Id == appointmentId);
        Assert.True(persisted.IsDeleted);
    }

    [Fact]
    public async Task Queue_soft_delete_is_filtered()
    {
        var department = CreateDepartment();
        var patient = CreatePatient();
        var queueEntryId = 0;
        await using (var context = fixture.CreateContext())
        {
            context.Departments.Add(department);
            context.Patients.Add(patient);
            await context.SaveChangesAsync();
            var entry = CreateQueueEntry(patient.Id, department.Id);
            context.WalkInQueueEntries.Add(entry);
            await context.SaveChangesAsync();
            queueEntryId = entry.Id;
            context.Entry(entry).Property(nameof(SoftDeletableEntity.IsDeleted)).CurrentValue = true;
            context.Entry(entry).Property(nameof(SoftDeletableEntity.DeletedAt)).CurrentValue = Utc(30);
            context.Entry(entry).Property(nameof(SoftDeletableEntity.DeletedBy)).CurrentValue = "integration";
            await context.SaveChangesAsync();
        }

        await using var readContext = fixture.CreateContext();
        Assert.Null(await readContext.WalkInQueueEntries.SingleOrDefaultAsync(item => item.Id == queueEntryId));
        var persisted = await readContext.WalkInQueueEntries.IgnoreQueryFilters()
            .SingleAsync(item => item.Id == queueEntryId);
        Assert.True(persisted.IsDeleted);
    }

    [Fact]
    public async Task Audit_values_roundtrip_without_identity_relationships()
    {
        var patient = CreatePatient();
        await using (var context = fixture.CreateContext())
        {
            context.Patients.Add(patient);
            await context.SaveChangesAsync();
            patient.UpdateContactDetails("0900555666", "Updated address", "Khartoum", null, null, null, null, Utc(15), "editor");
            await context.SaveChangesAsync();
        }

        await using var readContext = fixture.CreateContext();
        var reloaded = await readContext.Patients
            .IgnoreQueryFilters()
            .SingleAsync(item => item.Id == patient.Id);
        Assert.Equal("integration", reloaded.CreatedBy);
        Assert.Equal("editor", reloaded.UpdatedBy);
        Assert.Equal(Utc(15), reloaded.UpdatedAt);
    }

    private static Department CreateDepartment() =>
        new($"Department {Next()}", "Integration department", "201", Utc(20), "integration");

    private static Doctor CreateDoctor(Department department) =>
        new($"DR-{Next():D5}", "Integration Doctor", "Cardiology", false, 125.500m,
            department.Id, Utc(21), "integration");

    private static Patient CreatePatient(
        string? patientCode = null,
        string? phone = null,
        string? nationalId = null,
        string? passportNumber = null) =>
        new(
            patientCode ?? UniquePatientCode(),
            "First",
            "Middle",
            null,
            "Last",
            new DateOnly(1990, 4, 12),
            Gender.Female,
            BloodGroup.OPositive,
            nationalId,
            passportNumber,
            phone ?? UniquePhone(),
            "Integration address",
            "Khartoum",
            "Emergency Contact",
            "0900999888",
            "Sibling",
            "Provider",
            new DateOnly(2026, 12, 31),
            Utc(22),
            "integration");

    private static Appointment CreateAppointment(int patientId, int doctorId, int departmentId) =>
        new(
            UniqueCode("AP"),
            patientId,
            doctorId,
            departmentId,
            new DateOnly(2026, 11, 1),
            new TimeOnly(9, 0),
            AppointmentType.General,
            "integration appointment",
            Utc(23),
            "integration");

    private static WalkInQueueEntry CreateQueueEntry(int patientId, int departmentId) =>
        new(
            patientId,
            departmentId,
            new DateOnly(2026, 11, 2),
            7,
            "A-007",
            QueuePriority.Normal,
            "integration queue",
            Utc(24),
            "integration");

    private static string UniquePatientCode() => $"PT-2026-{Next():D5}";

    private static string UniquePhone() => $"0900{Next():D6}";

    private static string UniqueCode(string prefix) => $"{prefix}-{Next():D8}";

    private static int Next() => 10000 + (Interlocked.Increment(ref _sequence) % 89999);

    private static DateTimeOffset Utc(int second) =>
        new(2026, 1, 1, 12, 0, second, TimeSpan.Zero);
}
