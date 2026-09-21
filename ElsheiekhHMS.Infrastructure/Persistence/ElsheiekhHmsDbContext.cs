using ElsheiekhHMS.Core.Domain.Organization.Entities;
using ElsheiekhHMS.Core.Domain.Patients.Entities;
using ElsheiekhHMS.Core.Domain.Scheduling.Entities;
using ElsheiekhHMS.Core.Domain.Staff.Entities;
using Microsoft.EntityFrameworkCore;

namespace ElsheiekhHMS.Infrastructure.Persistence;

public sealed class ElsheiekhHmsDbContext(
    DbContextOptions<ElsheiekhHmsDbContext> options)
    : DbContext(options)
{
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Doctor> Doctors => Set<Doctor>();
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<WalkInQueueEntry> WalkInQueueEntries => Set<WalkInQueueEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ElsheiekhHmsDbContext).Assembly);
    }
}
