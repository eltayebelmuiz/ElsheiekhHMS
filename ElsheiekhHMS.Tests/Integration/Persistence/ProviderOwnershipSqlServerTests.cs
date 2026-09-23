using ElsheiekhHMS.Core.Domain.Organization.Entities;
using ElsheiekhHMS.Core.Domain.Staff.Entities;
using ElsheiekhHMS.Infrastructure.Identity.Entities;
using Microsoft.EntityFrameworkCore;
using ElsheiekhHMS.Infrastructure.Persistence;
using Xunit;

namespace ElsheiekhHMS.Tests.Integration.Persistence;

[Collection("SQL Server persistence")]
public sealed class ProviderOwnershipSqlServerTests(SqlServerTestDatabaseFixture fixture)
{
    private static int _sequence;

    [Fact]
    public async Task Ownership_roundtrips_and_unassignment_preserves_doctor_and_user()
    {
        var (department, doctor, user) = await SeedPrincipalsAsync();
        await using (var writeContext = fixture.CreateContext())
        {
            writeContext.DoctorApplicationUserLinks.Add(
                new DoctorApplicationUserLink(doctor.Id, user.Id));
            await writeContext.SaveChangesAsync();
        }

        await using (var removeContext = fixture.CreateContext())
        {
            var link = await removeContext.DoctorApplicationUserLinks
                .SingleAsync(item => item.DoctorId == doctor.Id);
            removeContext.DoctorApplicationUserLinks.Remove(link);
            await removeContext.SaveChangesAsync();
        }

        await using var readContext = fixture.CreateContext();
        Assert.Null(await readContext.DoctorApplicationUserLinks
            .SingleOrDefaultAsync(item => item.DoctorId == doctor.Id));
        Assert.NotNull(await readContext.Doctors.SingleOrDefaultAsync(item => item.Id == doctor.Id));
        Assert.NotNull(await readContext.Users.SingleOrDefaultAsync(item => item.Id == user.Id));
    }

    [Fact]
    public async Task Unique_user_index_rejects_two_doctors_for_one_user()
    {
        var (_, firstDoctor, user) = await SeedPrincipalsAsync();
        var secondDoctor = await SeedDoctorAsync();
        await using var context = fixture.CreateContext();
        context.DoctorApplicationUserLinks.Add(
            new DoctorApplicationUserLink(firstDoctor.Id, user.Id));
        await context.SaveChangesAsync();
        context.DoctorApplicationUserLinks.Add(
            new DoctorApplicationUserLink(secondDoctor.Id, user.Id));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task Doctor_primary_key_rejects_two_users_for_one_doctor()
    {
        var (_, doctor, firstUser) = await SeedPrincipalsAsync();
        var secondUser = await SeedUserAsync();
        await using (var firstContext = fixture.CreateContext())
        {
            firstContext.DoctorApplicationUserLinks.Add(
                new DoctorApplicationUserLink(doctor.Id, firstUser.Id));
            await firstContext.SaveChangesAsync();
        }

        await using var secondContext = fixture.CreateContext();
        secondContext.DoctorApplicationUserLinks.Add(
            new DoctorApplicationUserLink(doctor.Id, secondUser.Id));
        await Assert.ThrowsAsync<DbUpdateException>(() => secondContext.SaveChangesAsync());
    }

    [Fact]
    public async Task Restrictive_foreign_keys_preserve_history_when_principal_delete_is_attempted()
    {
        var (_, doctor, user) = await SeedPrincipalsAsync();
        await using (var seedContext = fixture.CreateContext())
        {
            seedContext.DoctorApplicationUserLinks.Add(
                new DoctorApplicationUserLink(doctor.Id, user.Id));
            await seedContext.SaveChangesAsync();
        }

        await using var deleteContext = fixture.CreateContext();
        deleteContext.Doctors.Remove(await deleteContext.Doctors.SingleAsync(item => item.Id == doctor.Id));
        await Assert.ThrowsAsync<DbUpdateException>(() => deleteContext.SaveChangesAsync());

        await using var userDeleteContext = fixture.CreateContext();
        userDeleteContext.Users.Remove(await userDeleteContext.Users.SingleAsync(item => item.Id == user.Id));
        await Assert.ThrowsAsync<DbUpdateException>(() => userDeleteContext.SaveChangesAsync());
    }

    private async Task<(Department Department, Doctor Doctor, ApplicationUser User)> SeedPrincipalsAsync()
    {
        var department = new Department(
            $"Ownership {Next()}", null, null, Utc(), "integration");
        await using (var context = fixture.CreateContext())
        {
            context.Departments.Add(department);
            await context.SaveChangesAsync();
        }

        var doctor = new Doctor(
            $"OWN-{Next():D6}", "Ownership Doctor", "Internal Medicine", false,
            100m, department.Id, Utc(), "integration");
        var user = NewUser();
        await using (var context = fixture.CreateContext())
        {
            context.Doctors.Add(doctor);
            context.Users.Add(user);
            await context.SaveChangesAsync();
        }

        return (department, doctor, user);
    }

    private async Task<Doctor> SeedDoctorAsync()
    {
        var department = new Department(
            $"Ownership {Next()}", null, null, Utc(), "integration");
        await using var context = fixture.CreateContext();
        context.Departments.Add(department);
        await context.SaveChangesAsync();
        var doctor = new Doctor(
            $"OWN-{Next():D6}", "Ownership Doctor", "Internal Medicine", false,
            100m, department.Id, Utc(), "integration");
        context.Doctors.Add(doctor);
        await context.SaveChangesAsync();
        return doctor;
    }

    private async Task<ApplicationUser> SeedUserAsync()
    {
        var user = NewUser();
        await using var context = fixture.CreateContext();
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }

    private static ApplicationUser NewUser()
    {
        var suffix = Next().ToString("D8");
        return new ApplicationUser
        {
            Id = $"integration-user-{suffix}",
            UserName = $"integration-{suffix}@example.local",
            NormalizedUserName = $"INTEGRATION-{suffix}@EXAMPLE.LOCAL",
            Email = $"integration-{suffix}@example.local",
            NormalizedEmail = $"INTEGRATION-{suffix}@EXAMPLE.LOCAL",
            EmailConfirmed = true,
            DisplayName = "Integration Provider",
            CreatedAt = Utc(),
            LoginAllowed = true
        };
    }

    private static int Next() => 10000 + Interlocked.Increment(ref _sequence);

    private static DateTimeOffset Utc() =>
        new DateTimeOffset(2026, 1, 2, 12, 0, 0, TimeSpan.Zero).AddSeconds(_sequence);
}
