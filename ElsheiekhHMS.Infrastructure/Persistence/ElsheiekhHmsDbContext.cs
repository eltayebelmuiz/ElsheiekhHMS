using ElsheiekhHMS.Core.Domain.Organization.Entities;
using ElsheiekhHMS.Core.Domain.Patients.Entities;
using ElsheiekhHMS.Core.Domain.Scheduling.Entities;
using ElsheiekhHMS.Core.Domain.Staff.Entities;
using ElsheiekhHMS.Infrastructure.Auditing.Entities;
using ElsheiekhHMS.Infrastructure.Identity.Entities;
using ElsheiekhHMS.Infrastructure.Persistence.Allocation;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ElsheiekhHMS.Infrastructure.Persistence;

public sealed class ElsheiekhHmsDbContext(
    DbContextOptions<ElsheiekhHmsDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Doctor> Doctors => Set<Doctor>();
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<WalkInQueueEntry> WalkInQueueEntries => Set<WalkInQueueEntry>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<PatientCodeAllocation> PatientCodeAllocations => Set<PatientCodeAllocation>();
    public DbSet<QueueTicketAllocation> QueueTicketAllocations => Set<QueueTicketAllocation>();
    public DbSet<AppointmentCodeAllocation> AppointmentCodeAllocations => Set<AppointmentCodeAllocation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ElsheiekhHmsDbContext).Assembly);
    }
}
