using ElsheiekhHMS.Core.Domain.Organization.Entities;
using ElsheiekhHMS.Core.Domain.Patients.Entities;
using ElsheiekhHMS.Core.Domain.Scheduling.Entities;
using ElsheiekhHMS.Core.Domain.Staff.Entities;
using ElsheiekhHMS.Infrastructure.Persistence;
using ElsheiekhHMS.Infrastructure.Persistence.Allocation;
using Microsoft.EntityFrameworkCore;

namespace ElsheiekhHMS.Tests.Unit.Infrastructure;

public sealed class ElsheiekhHmsDbContextTests
{
    [Fact]
    public void Context_exposes_only_approved_phase04_persistent_roots()
    {
        var options = new DbContextOptionsBuilder<ElsheiekhHmsDbContext>()
            .UseSqlServer("Server=unused;Database=ShapeOnly;")
            .Options;

        using var context = new ElsheiekhHmsDbContext(options);

        var dbSetProperties = typeof(ElsheiekhHmsDbContext)
            .GetProperties()
            .Where(property => property.PropertyType.IsGenericType)
            .Where(property => property.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
            .ToDictionary(property => property.Name, property => property.PropertyType);

        Assert.Equal(typeof(DbSet<Department>), dbSetProperties[nameof(context.Departments)]);
        Assert.Equal(typeof(DbSet<Doctor>), dbSetProperties[nameof(context.Doctors)]);
        Assert.Equal(typeof(DbSet<Patient>), dbSetProperties[nameof(context.Patients)]);
        Assert.Equal(typeof(DbSet<Appointment>), dbSetProperties[nameof(context.Appointments)]);
        Assert.Equal(typeof(DbSet<WalkInQueueEntry>), dbSetProperties[nameof(context.WalkInQueueEntries)]);
        Assert.Equal(typeof(DbSet<PatientCodeAllocation>), dbSetProperties[nameof(context.PatientCodeAllocations)]);
        Assert.Equal(typeof(DbSet<QueueTicketAllocation>), dbSetProperties[nameof(context.QueueTicketAllocations)]);
        Assert.Equal(typeof(DbSet<AppointmentCodeAllocation>), dbSetProperties[nameof(context.AppointmentCodeAllocations)]);
        Assert.Equal(typeof(DbSet<ElsheiekhHMS.Core.Domain.Clinical.Entities.Encounter>),
            dbSetProperties[nameof(context.Encounters)]);
        Assert.DoesNotContain(dbSetProperties.Keys, name =>
            name is "Admissions" or "ClinicalObservations");
    }
}
