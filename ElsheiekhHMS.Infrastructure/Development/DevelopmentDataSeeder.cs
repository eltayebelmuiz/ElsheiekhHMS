using ElsheiekhHMS.Application.Appointments;
using ElsheiekhHMS.Application.Appointments.Contracts;
using ElsheiekhHMS.Application.Appointments.Persistence;
using ElsheiekhHMS.Application.Common.Auditing;
using ElsheiekhHMS.Application.Common.Security;
using ElsheiekhHMS.Application.Clinical.ProviderOwnership;
using ElsheiekhHMS.Application.Clinical.ProviderOwnership.Contracts;
using ElsheiekhHMS.Application.Clinical.ProviderOwnership.Persistence;
using ElsheiekhHMS.Application.Departments;
using ElsheiekhHMS.Application.Departments.Contracts;
using ElsheiekhHMS.Application.Departments.Persistence;
using ElsheiekhHMS.Application.Patients;
using ElsheiekhHMS.Application.Patients.Contracts;
using ElsheiekhHMS.Application.Patients.Persistence;
using ElsheiekhHMS.Application.Queue;
using ElsheiekhHMS.Application.Queue.Contracts;
using ElsheiekhHMS.Application.Queue.Persistence;
using ElsheiekhHMS.Application.Workflows.AppointmentArrival;
using ElsheiekhHMS.Core.Domain.Patients.Enums;
using ElsheiekhHMS.Core.Domain.Scheduling.Entities;
using ElsheiekhHMS.Core.Domain.Scheduling.Enums;
using ElsheiekhHMS.Core.Domain.Staff.Entities;
using ElsheiekhHMS.Core.Domain.Staff.Enums;
using ElsheiekhHMS.Infrastructure.Identity;
using ElsheiekhHMS.Infrastructure.Identity.Entities;
using ElsheiekhHMS.Infrastructure.Persistence;
using ElsheiekhHMS.Infrastructure.Persistence.Allocation;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;

namespace ElsheiekhHMS.Infrastructure.Development;

/// <summary>
/// Explicit, repeatable Development-only data for manual HMS testing. This is
/// never invoked unless the Development environment and the secret-backed flag
/// are both enabled, and it refuses every database other than ElsheiekhHMS_Dev.
/// </summary>
public sealed class DevelopmentDataSeeder(
    IWebHostEnvironment environment,
    IConfiguration configuration,
    ElsheiekhHmsDbContext context,
    UserManager<ApplicationUser> userManager,
    IdentityRoleSeeder roleSeeder,
    IDepartmentPersistence departmentPersistence,
    IPatientPersistence patientPersistence,
    IAppointmentPersistence appointmentPersistence,
    IQueuePersistence queuePersistence,
    IAuditEventWriter auditEventWriter,
    IProviderOwnershipPersistence providerOwnershipPersistence,
    TimeProvider timeProvider,
    AppointmentCodeAllocator appointmentCodeAllocator,
    ILogger<DevelopmentDataSeeder> logger)
{
    private const string DevelopmentDatabaseName = "ElsheiekhHMS_Dev";
    private const string HospitalTimeZoneId = "Africa/Kigali";
    private const string SeedMarker = "DEV-SEED";

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (!string.Equals(environment.EnvironmentName, Environments.Development, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Development data seeding is only available in the Development environment.");
        }

        if (!configuration.GetValue<bool>("DevelopmentSeed:Enabled"))
        {
            throw new InvalidOperationException("Development data seeding requires DevelopmentSeed:Enabled=true in User Secrets.");
        }

        var database = context.Database.GetDbConnection().Database;
        if (!string.Equals(database, DevelopmentDatabaseName, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Development seeding refused database '{database}'. Expected '{DevelopmentDatabaseName}'.");
        }

        var pending = (await context.Database.GetPendingMigrationsAsync(cancellationToken)).ToArray();
        if (pending.Length > 0)
        {
            throw new InvalidOperationException("Development seeding requires an up-to-date database. Pending migrations: " + string.Join(", ", pending));
        }

        await roleSeeder.SeedAsync(cancellationToken);
        var users = await SeedUsersAsync(cancellationToken);
        var actor = new SeedCurrentUser(users[RoleNames.Administrator].Id, users[RoleNames.Administrator].UserName!);

        await SeedDepartmentsAsync(actor, cancellationToken);
        await SeedDoctorsAsync(actor, cancellationToken);
        await SeedProviderOwnershipAsync(actor, users[RoleNames.Provider].Id, cancellationToken);
        var patients = await SeedPatientsAsync(actor, cancellationToken);
        var appointments = await SeedAppointmentsAsync(actor, patients, cancellationToken);
        await SeedQueueAsync(actor, patients, appointments, cancellationToken);

        logger.LogInformation("Development data seed completed for {Database}.", database);
    }

    private async Task<Dictionary<string, ApplicationUser>> SeedUsersAsync(CancellationToken cancellationToken)
    {
        var definitions = new[]
        {
            (RoleNames.SystemAdministrator, "sysadmin.dev@elsheiekh.local", "Development System Administrator"),
            (RoleNames.Administrator, "admin.dev@elsheiekh.local", "Development Administrator"),
            (RoleNames.Receptionist, "reception.dev@elsheiekh.local", "Development Receptionist"),
            (RoleNames.Provider, "provider.dev@elsheiekh.local", "Development Provider"),
            (RoleNames.Patient, "patient.dev@elsheiekh.local", "Development Patient")
        };
        var result = new Dictionary<string, ApplicationUser>(StringComparer.Ordinal);

        foreach (var (role, username, displayName) in definitions)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var user = await userManager.FindByNameAsync(username);
            if (user is null)
            {
                var password = configuration[$"DevelopmentSeed:Users:{role}:Password"];
                if (string.IsNullOrWhiteSpace(password))
                {
                    throw new InvalidOperationException($"Missing User Secret DevelopmentSeed:Users:{role}:Password.");
                }

                user = new ApplicationUser
                {
                    UserName = username,
                    Email = username,
                    EmailConfirmed = true,
                    DisplayName = displayName,
                    CreatedAt = timeProvider.GetUtcNow().ToUniversalTime(),
                    SecurityState = AccountSecurityState.Active,
                    LoginAllowed = true
                };
                var create = await userManager.CreateAsync(user, password);
                EnsureIdentitySuccess(create, $"create development user '{username}'");
            }

            if (!await userManager.IsInRoleAsync(user, role))
            {
                var addRole = await userManager.AddToRoleAsync(user, role);
                EnsureIdentitySuccess(addRole, $"assign role '{role}' to '{username}'");
            }

            result[role] = user;
        }

        return result;
    }

    private async Task SeedDepartmentsAsync(SeedCurrentUser actor, CancellationToken cancellationToken)
    {
        var service = new DepartmentService(departmentPersistence, auditEventWriter, actor, timeProvider);
        var definitions = new[]
        {
            ("General Medicine", "Primary and adult medicine", "101"),
            ("Pediatrics", "Children and adolescent care", "102"),
            ("Obstetrics & Gynecology", "Maternal and women's health", "103"),
            ("Surgery", "General surgical services", "104"),
            ("Orthopedics", "Bone and joint care", "105"),
            ("ENT", "Ear, nose and throat care", "106"),
            ("Ophthalmology", "Eye care services", "107"),
            ("Dental", "Oral health services", "108")
        };

        foreach (var definition in definitions)
        {
            if (await context.Departments.AnyAsync(d => d.Name == definition.Item1, cancellationToken))
            {
                continue;
            }

            var created = await service.CreateAsync(
                new CreateDepartmentRequest(definition.Item1, definition.Item2, definition.Item3),
                cancellationToken);
            EnsureSuccess(created, $"create department '{definition.Item1}'");
        }
    }

    private async Task SeedDoctorsAsync(SeedCurrentUser actor, CancellationToken cancellationToken)
    {
        var departmentIds = await context.Departments
            .OrderBy(d => d.Id)
            .Select(d => d.Id)
            .ToListAsync(cancellationToken);
        if (departmentIds.Count < 8)
        {
            throw new InvalidOperationException("The development department seed did not produce the expected departments.");
        }

        var definitions = new[]
        {
            ("DOC-001", "Aline Uwimana", "Internal Medicine", false, 35m),
            ("DOC-002", "Jean Claude Niyonzima", null, true, 25m),
            ("DOC-003", "Claudine Mukamana", "Pediatrics", false, 30m),
            ("DOC-004", "Emmanuel Habimana", "General Surgery", false, 50m),
            ("DOC-005", "Grace Ingabire", "Obstetrics", false, 45m),
            ("DOC-006", "Patrick Tuyisenge", "Orthopedics", false, 45m),
            ("DOC-007", "Mireille Nyirabazungu", "ENT", false, 30m),
            ("DOC-008", "Samuel Murenzi", "Ophthalmology", false, 35m),
            ("DOC-009", "Diane Uwamahoro", "Dentistry", false, 30m),
            ("DOC-010", "Eric Rukundo", null, true, 25m),
            ("DOC-011", "Beata Mukamana", "Internal Medicine", false, 35m),
            ("DOC-012", "Olivier Ndayisenga", "Pediatrics", false, 30m)
        };

        var now = timeProvider.GetUtcNow().ToUniversalTime();
        foreach (var (code, name, specialty, gp, fee) in definitions)
        {
            if (await context.Doctors.AnyAsync(d => d.DoctorCode == code, cancellationToken))
            {
                continue;
            }

            var doctor = new Doctor(code, name, specialty, gp, fee,
                departmentIds[(int.Parse(code[^3..], System.Globalization.CultureInfo.InvariantCulture) - 1) % departmentIds.Count], now, actor.UserId);
            doctor.AddSchedule(DayOfWeek.Monday, new TimeOnly(8, 0), new TimeOnly(16, 0), 30, now, actor.UserId);
            context.Doctors.Add(doctor);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedProviderOwnershipAsync(
        SeedCurrentUser actor,
        string providerUserId,
        CancellationToken cancellationToken)
    {
        var doctor = await context.Doctors
            .SingleOrDefaultAsync(item => item.DoctorCode == "DOC-001", cancellationToken);
        if (doctor is null)
        {
            throw new InvalidOperationException("The deterministic provider Doctor seed was not found.");
        }

        var service = new ProviderOwnershipService(
            providerOwnershipPersistence,
            auditEventWriter,
            actor);
        var result = await service.AssignAsync(
            new AssignProviderOwnershipRequest(doctor.Id, providerUserId),
            cancellationToken);
        EnsureSuccess(result, "assign the Development Provider account to Doctor DOC-001");
    }

    private async Task<List<int>> SeedPatientsAsync(SeedCurrentUser actor, CancellationToken cancellationToken)
    {
        var service = new PatientService(patientPersistence, auditEventWriter, actor, timeProvider);
        var ids = await context.Patients.OrderBy(p => p.Id).Select(p => p.Id).ToListAsync(cancellationToken);
        var firstNames = new[] { "Amani", "Chantal", "David", "Elise", "Fabrice", "Grace", "Hassan", "Irene", "Jean", "Keza", "Louis", "Maya", "Nadia", "Olivier", "Peace", "Samuel", "Théo", "Uwase", "Victor", "Yvette" };
        var lastNames = new[] { "Mukamana", "Niyonsenga", "Uwimana", "Habimana", "Mugisha", "Nshimiyimana", "Ingabire", "Tuyisenge", "Mutesi", "Bizimana" };
        var today = KigaliDate(timeProvider.GetUtcNow());

        for (var i = 1; i <= 50; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var nationalId = $"DEV-RW-{i:D4}";
            var existing = await context.Patients.SingleOrDefaultAsync(p => p.NationalId == nationalId, cancellationToken);
            if (existing is not null)
            {
                if (!ids.Contains(existing.Id)) ids.Add(existing.Id);
                continue;
            }

            var first = firstNames[(i - 1) % firstNames.Length];
            var last = lastNames[(i * 3 - 1) % lastNames.Length];
            var birth = today.AddYears(-(18 + (i * 7 % 58))).AddDays(-(i * 11 % 300));
            var phone = i % 10 == 0 ? "+250788000001" : $"+25078800{i:D4}";
            var request = new RegisterPatientRequest(
                first, i % 3 == 0 ? "Marie" : null, i % 7 == 0 ? "Jean" : null, last,
                birth, i % 2 == 0 ? Gender.Female : Gender.Male,
                (BloodGroup)(i % 8), nationalId, i % 9 == 0 ? $"DEV-PASSPORT-{i:D4}" : null,
                phone, $"{100 + i} Kigali Heights", "Kigali", "Emergency Contact", "+250788999999", "Family", i % 4 == 0 ? "RwandaCare" : null);
            var created = await service.RegisterAsync(request, cancellationToken);
            EnsureSuccess(created, $"create patient seed {i}");
            if (created.Value is not null) ids.Add(created.Value.Id);
        }

        return ids.OrderBy(id => id).ToList();
    }

    private async Task<List<int>> SeedAppointmentsAsync(
        SeedCurrentUser actor,
        IReadOnlyList<int> patientIds,
        CancellationToken cancellationToken)
    {
        var departments = await context.Departments.OrderBy(d => d.Id).Select(d => d.Id).ToListAsync(cancellationToken);
        var doctors = await context.Doctors.Where(d => d.Status == DoctorStatus.Active).OrderBy(d => d.Id).ToListAsync(cancellationToken);
        var existing = await context.Appointments
            .Where(a => a.Notes != null && a.Notes.StartsWith(SeedMarker + "-APPT-"))
            .OrderBy(a => a.Id)
            .Select(a => a.Id)
            .ToListAsync(cancellationToken);
        if (existing.Count >= 36)
        {
            return existing;
        }

        var now = timeProvider.GetUtcNow().ToUniversalTime();
        var today = KigaliDate(now);
        var ids = existing.ToHashSet();
        for (var i = 1; i <= 36; i++)
        {
            if (await context.Appointments.AnyAsync(a => a.Notes == $"{SeedMarker}-APPT-{i:D3}", cancellationToken))
            {
                continue;
            }

            var date = i <= 12 ? today.AddDays(-i) : i <= 24 ? today : today.AddDays(i - 24 + 1);
            var time = new TimeOnly(8 + (i % 8), (i * 15) % 60);
            var departmentId = departments[(i - 1) % departments.Count];
            var doctor = doctors.First(d => d.DepartmentId == departmentId);
            var code = await appointmentCodeAllocator.AllocateAsync(cancellationToken);
            var appointment = new Appointment(code, patientIds[(i + 9) % patientIds.Count], doctor.Id, departmentId,
                date, time, (AppointmentType)((i - 1) % Enum.GetValues<AppointmentType>().Length),
                $"{SeedMarker}-APPT-{i:D3}", now, actor.UserId);

            switch (i % 6)
            {
                case 1: appointment.Confirm(now, actor.UserId); break;
                case 2: appointment.CheckIn(now, actor.UserId); break;
                case 3: appointment.CheckIn(now, actor.UserId); appointment.Complete(now, actor.UserId); break;
                case 4: appointment.Cancel("Development history", now, actor.UserId); break;
                case 5: appointment.MarkNoShow(now, actor.UserId); break;
            }

            context.Appointments.Add(appointment);
            await context.SaveChangesAsync(cancellationToken);
            ids.Add(appointment.Id);
        }

        return ids.OrderBy(id => id).ToList();
    }

    private async Task SeedQueueAsync(
        SeedCurrentUser actor,
        IReadOnlyList<int> patientIds,
        IReadOnlyList<int> appointmentIds,
        CancellationToken cancellationToken)
    {
        var fakeAppointmentService = new AppointmentService(appointmentPersistence, auditEventWriter, actor, timeProvider);
        var fakeQueueService = new QueueService(queuePersistence, auditEventWriter, actor, timeProvider);
        var arrival = new AppointmentArrivalQueueService(fakeAppointmentService, fakeQueueService, actor);
        var departmentIds = await context.Departments.OrderBy(d => d.Id).Select(d => d.Id).ToListAsync(cancellationToken);
        var doctors = await context.Doctors.Where(d => d.Status == DoctorStatus.Active).OrderBy(d => d.Id).ToListAsync(cancellationToken);
        for (var i = 1; i <= 6; i++)
        {
            var marker = $"{SeedMarker}-WALKIN-{i:D3}";
            var existing = await context.WalkInQueueEntries
                .SingleOrDefaultAsync(q => q.Notes == marker, cancellationToken);
            if (existing is not null)
            {
                if (i == 3 && existing.Status == QueueStatus.AtDoctor)
                {
                    var current = await fakeQueueService.GetByIdAsync(existing.Id, cancellationToken);
                    if (current.IsSuccess && current.Value is not null)
                    {
                        var completed = await fakeQueueService.CompleteAsync(
                            new QueueEntryActionRequest(current.Value.Id, current.Value.ConcurrencyToken),
                            cancellationToken);
                        EnsureSuccess(completed, "complete walk-in queue seed 3");
                    }
                }

                continue;
            }
            var result = await fakeQueueService.AddAsync(
                new AddWalkInQueueEntryRequest(patientIds[i - 1], departmentIds[(i - 1) % departmentIds.Count],
                    (QueuePriority)((i - 1) % 3), marker), cancellationToken);
            EnsureSuccess(result, $"create walk-in queue seed {i}");
            if (result.Value is null) continue;
            if (i == 2)
            {
                result = await fakeQueueService.CallToNurseAsync(new QueueEntryActionRequest(result.Value.Id, result.Value.ConcurrencyToken), cancellationToken);
            }
            else if (i == 3)
            {
                result = await fakeQueueService.CallToNurseAsync(new QueueEntryActionRequest(result.Value.Id, result.Value.ConcurrencyToken), cancellationToken);
                if (result.Value is not null)
                {
                    var doctor = doctors.First(d => d.DepartmentId == result.Value.DepartmentId);
                    result = await fakeQueueService.SendToDoctorAsync(new SendToDoctorRequest(result.Value.Id, doctor.Id, result.Value.ConcurrencyToken), cancellationToken);
                }
            }
            else if (i == 4)
            {
                result = await fakeQueueService.CancelAsync(new QueueEntryActionRequest(result.Value.Id, result.Value.ConcurrencyToken), cancellationToken);
            }
            else if (i == 5)
            {
                result = await fakeQueueService.HoldAsync(new QueueEntryActionRequest(result.Value.Id, result.Value.ConcurrencyToken), cancellationToken);
            }
            EnsureSuccess(result, $"transition walk-in queue seed {i}");
        }

        var linked = await context.WalkInQueueEntries
            .Where(q => q.AppointmentId != null && q.Notes != null && q.Notes.StartsWith(SeedMarker + "-LINKED-"))
            .Select(q => q.AppointmentId!.Value)
            .ToListAsync(cancellationToken);
        var candidates = appointmentIds.Where(id => !linked.Contains(id)).Take(3).ToArray();
        foreach (var appointmentId in candidates)
        {
            var appointment = await fakeAppointmentService.GetByIdAsync(appointmentId, cancellationToken);
            if (!appointment.IsSuccess || appointment.Value is null || appointment.Value.Status is AppointmentStatus.Cancelled or AppointmentStatus.NoShow or AppointmentStatus.Completed)
            {
                continue;
            }

            var marker = $"{SeedMarker}-LINKED-{appointmentId:D3}";
            var result = await arrival.CheckInAndQueueAsync(
                new AppointmentArrivalQueueRequest(appointmentId, appointment.Value.ConcurrencyToken,
                    QueuePriority.Urgent, marker), cancellationToken);
            EnsureSuccess(result, $"create appointment-linked queue seed {appointmentId}");
        }

        var linkedForDoctor = await context.WalkInQueueEntries
            .Where(q => q.AppointmentId != null && q.Notes != null && q.Notes.StartsWith(SeedMarker + "-LINKED-"))
            .OrderBy(q => q.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (linkedForDoctor?.Status == QueueStatus.Waiting)
        {
            var current = await fakeQueueService.GetByIdAsync(linkedForDoctor.Id, cancellationToken);
            if (current.IsSuccess && current.Value is not null)
            {
                var doctor = doctors.First(d => d.DepartmentId == current.Value.DepartmentId);
                var atDoctor = await fakeQueueService.SendToDoctorAsync(
                    new SendToDoctorRequest(current.Value.Id, doctor.Id, current.Value.ConcurrencyToken),
                    cancellationToken);
                EnsureSuccess(atDoctor, "send appointment-linked queue seed to doctor");
            }
        }
    }

    private static DateOnly KigaliDate(DateTimeOffset utcNow) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeBySystemTimeZoneId(utcNow, HospitalTimeZoneId).DateTime);

    private static void EnsureIdentitySuccess(IdentityResult result, string operation)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"Unable to {operation}: {string.Join(", ", result.Errors.Select(e => e.Code))}");
        }
    }

    private static void EnsureSuccess<T>(ElsheiekhHMS.Application.Common.Results.ServiceResult<T> result, string operation)
    {
        if (!result.IsSuccess)
        {
            throw new InvalidOperationException($"Unable to {operation}: {string.Join(", ", result.Errors.Select(e => e.Code))}");
        }
    }

    private sealed class SeedCurrentUser(string userId, string userName) : ICurrentUser
    {
        public bool IsAuthenticated => true;
        public string? UserId { get; } = userId;
        public string? UserName { get; } = userName;
        public IReadOnlyCollection<string> Roles { get; } =
            [RoleNames.SystemAdministrator, RoleNames.Administrator, RoleNames.Receptionist];
    }
}
