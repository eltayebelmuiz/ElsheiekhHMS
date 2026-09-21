using ElsheiekhHMS.Core.Domain.Organization.Entities;
using ElsheiekhHMS.Core.Domain.Patients.Entities;
using ElsheiekhHMS.Core.Domain.Scheduling.Entities;
using ElsheiekhHMS.Core.Domain.Staff.Entities;
using ElsheiekhHMS.Infrastructure.Identity.Entities;
using ElsheiekhHMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ElsheiekhHMS.Tests.Unit.Infrastructure.Identity;

public sealed class IdentityModelTests
{
    [Fact]
    public void Identity_model_preserves_existing_domain_entities_and_adds_identity_entities()
    {
        using var context = new ElsheiekhHmsDbContext(
            new DbContextOptionsBuilder<ElsheiekhHmsDbContext>()
                .UseSqlServer("Server=unused;Database=ShapeOnly;")
                .Options);

        var entityTypes = context.Model.GetEntityTypes().ToArray();

        Assert.Contains(entityTypes, entity => entity.ClrType == typeof(ApplicationUser));
        Assert.Contains(entityTypes, entity => entity.ClrType == typeof(Department));
        Assert.Contains(entityTypes, entity => entity.ClrType == typeof(Doctor));
        Assert.Contains(entityTypes, entity => entity.ClrType == typeof(DoctorSchedule));
        Assert.Contains(entityTypes, entity => entity.ClrType == typeof(Patient));
        Assert.Contains(entityTypes, entity => entity.ClrType == typeof(Appointment));
        Assert.Contains(entityTypes, entity => entity.ClrType == typeof(WalkInQueueEntry));
        var user = entityTypes.Single(entity => entity.ClrType == typeof(ApplicationUser));
        Assert.Equal("AspNetUsers", user.GetTableName());
        Assert.Equal("tinyint", user.FindProperty(nameof(ApplicationUser.SecurityState))!.GetColumnType());
        Assert.Equal(AccountSecurityState.Active, user.FindProperty(nameof(ApplicationUser.SecurityState))!.GetDefaultValue());
        Assert.Equal("bit", user.FindProperty(nameof(ApplicationUser.LoginAllowed))!.GetColumnType());
        Assert.Equal(true, user.FindProperty(nameof(ApplicationUser.LoginAllowed))!.GetDefaultValue());
    }
}
