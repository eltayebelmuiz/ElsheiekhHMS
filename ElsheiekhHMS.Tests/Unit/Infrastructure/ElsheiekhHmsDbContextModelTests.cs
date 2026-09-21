using ElsheiekhHMS.Core.Domain.Organization.Entities;
using ElsheiekhHMS.Core.Domain.Patients.Entities;
using ElsheiekhHMS.Core.Domain.Staff.Entities;
using ElsheiekhHMS.Core.Domain.Scheduling.Entities;
using ElsheiekhHMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Reflection;

namespace ElsheiekhHMS.Tests.Unit.Infrastructure;

public sealed class ElsheiekhHmsDbContextModelTests
{
    [Fact]
    public void Model_contains_all_approved_persistent_entities()
    {
        using var context = CreateContext();

        var entityNames = context.Model.GetEntityTypes()
            .Select(entity => entity.ClrType.Name)
            .OrderBy(name => name)
            .ToArray();

        Assert.Equal(
            ["Appointment", "Department", "Doctor", "DoctorSchedule", "Patient", "WalkInQueueEntry"],
            entityNames);
    }

    [Fact]
    public void PatientCode_is_mapped_through_its_existing_backing_field()
    {
        using var context = CreateContext();

        var patient = context.Model.FindEntityType(typeof(Patient))!;
        var property = patient.FindProperty(nameof(Patient.PatientCode));

        Assert.NotNull(property);
        Assert.Equal("<PatientCode>k__BackingField", property!.GetFieldName());
    }

    [Fact]
    public void Scalar_metadata_preserves_the_approved_phase04b_shape()
    {
        using var context = CreateContext();

        var patient = context.Model.FindEntityType(typeof(Patient))!;
        var doctor = context.Model.FindEntityType(typeof(Doctor))!;
        var department = context.Model.FindEntityType(typeof(Department))!;
        var schedule = context.Model.FindEntityType(typeof(DoctorSchedule))!;
        var appointment = context.Model.FindEntityType(typeof(Appointment))!;
        var queueEntry = context.Model.FindEntityType(typeof(WalkInQueueEntry))!;

        Assert.Equal("Departments", department.GetTableName());
        Assert.Equal("DoctorSchedules", schedule.GetTableName());
        Assert.Equal("datetimeoffset(7)", department.FindProperty(nameof(Department.CreatedAt))!.GetColumnType());
        Assert.Equal("datetimeoffset(7)", schedule.FindProperty(nameof(DoctorSchedule.CreatedAt))!.GetColumnType());
        Assert.Equal("Patients", patient.GetTableName());
        Assert.Equal(13, patient.FindProperty(nameof(Patient.PatientCode))!.GetMaxLength());
        Assert.True(patient.FindProperty(nameof(Patient.PatientCode))!.IsUnicode());
        Assert.Equal("date", patient.FindProperty(nameof(Patient.DateOfBirth))!.GetColumnType());
        Assert.Equal(typeof(int), patient.FindProperty(nameof(Patient.Gender))!.GetRelationalTypeMapping().Converter!.ProviderClrType);
        Assert.Equal(typeof(int), patient.FindProperty(nameof(Patient.BloodGroup))!.GetRelationalTypeMapping().Converter!.ProviderClrType);
        Assert.Null(patient.FindProperty(nameof(Patient.FullName)));
        Assert.Null(patient.FindProperty(nameof(Patient.ShortName)));

        Assert.Equal(12, doctor.FindProperty(nameof(Doctor.ConsultationFee))!.GetPrecision());
        Assert.Equal(3, doctor.FindProperty(nameof(Doctor.ConsultationFee))!.GetScale());
        Assert.Equal(typeof(int), doctor.FindProperty(nameof(Doctor.Status))!.GetRelationalTypeMapping().Converter!.ProviderClrType);

        Assert.Equal("date", appointment.FindProperty(nameof(Appointment.ScheduledDate))!.GetColumnType());
        Assert.Equal("time(0)", appointment.FindProperty(nameof(Appointment.ScheduledTime))!.GetColumnType());
        Assert.Equal("datetimeoffset(7)", appointment.FindProperty(nameof(Appointment.CancelledAt))!.GetColumnType());
        Assert.Equal(typeof(int), appointment.FindProperty(nameof(Appointment.Type))!.GetRelationalTypeMapping().Converter!.ProviderClrType);
        Assert.Equal(typeof(int), appointment.FindProperty(nameof(Appointment.Status))!.GetRelationalTypeMapping().Converter!.ProviderClrType);

        Assert.Equal("date", queueEntry.FindProperty(nameof(WalkInQueueEntry.QueueDate))!.GetColumnType());
        Assert.Equal("datetimeoffset(7)", queueEntry.FindProperty(nameof(WalkInQueueEntry.RegisteredAt))!.GetColumnType());
        Assert.Equal(typeof(int), queueEntry.FindProperty(nameof(WalkInQueueEntry.Priority))!.GetRelationalTypeMapping().Converter!.ProviderClrType);
        Assert.Equal(typeof(int), queueEntry.FindProperty(nameof(WalkInQueueEntry.Status))!.GetRelationalTypeMapping().Converter!.ProviderClrType);
    }

    [Fact]
    public void Relationships_use_required_cardinality_and_restrict_delete_behavior()
    {
        using var context = CreateContext();

        AssertRelationship<Department, Doctor>(context, nameof(Doctor.DepartmentId), required: true);
        AssertRelationship<Doctor, DoctorSchedule>(context, "DoctorId", required: true);
        AssertRelationship<Patient, Appointment>(context, nameof(Appointment.PatientId), required: true);
        AssertRelationship<Doctor, Appointment>(context, nameof(Appointment.DoctorId), required: true);
        AssertRelationship<Department, Appointment>(context, nameof(Appointment.DepartmentId), required: true);
        AssertRelationship<Patient, WalkInQueueEntry>(context, nameof(WalkInQueueEntry.PatientId), required: true);
        AssertRelationship<Doctor, WalkInQueueEntry>(context, nameof(WalkInQueueEntry.DoctorId), required: false);
        AssertRelationship<Department, WalkInQueueEntry>(context, nameof(WalkInQueueEntry.DepartmentId), required: true);
    }

    [Fact]
    public void Approved_filters_indexes_and_uniqueness_rules_are_present()
    {
        using var context = CreateContext();

        foreach (var type in new[] { typeof(Patient), typeof(Doctor), typeof(Appointment), typeof(WalkInQueueEntry) })
        {
            var query = type switch
            {
                var patientType when patientType == typeof(Patient) => context.Set<Patient>().ToQueryString(),
                var doctorType when doctorType == typeof(Doctor) => context.Set<Doctor>().ToQueryString(),
                var appointmentType when appointmentType == typeof(Appointment) => context.Set<Appointment>().ToQueryString(),
                _ => context.Set<WalkInQueueEntry>().ToQueryString()
            };
            Assert.Contains("IsDeleted", query, StringComparison.Ordinal);
        }

        foreach (var type in new[] { typeof(Department), typeof(DoctorSchedule) })
        {
            var query = type == typeof(Department) ? context.Set<Department>().ToQueryString()
                : context.Set<DoctorSchedule>().ToQueryString();
            Assert.DoesNotContain("IsDeleted", query, StringComparison.Ordinal);
        }

        var patient = context.Model.FindEntityType(typeof(Patient))!;
        AssertIndex(patient, [nameof(Patient.PatientCode)], unique: true);
        AssertIndex(patient, [nameof(Patient.NationalId)], unique: true, filter: "[NationalId] IS NOT NULL");
        AssertIndex(patient, [nameof(Patient.PassportNumber)], unique: true, filter: "[PassportNumber] IS NOT NULL");
        AssertIndex(patient, [nameof(Patient.Phone)], unique: false);

        var appointment = context.Model.FindEntityType(typeof(Appointment))!;
        AssertIndex(appointment,
            [nameof(Appointment.DoctorId), nameof(Appointment.ScheduledDate), nameof(Appointment.ScheduledTime), nameof(Appointment.Status)],
            unique: false);
        AssertIndex(appointment, [nameof(Appointment.PatientId), nameof(Appointment.ScheduledDate)], unique: false);
        AssertIndex(appointment, [nameof(Appointment.DepartmentId)], unique: false);

        var queue = context.Model.FindEntityType(typeof(WalkInQueueEntry))!;
        AssertIndex(queue, [nameof(WalkInQueueEntry.PatientId)], unique: false);
        AssertIndex(queue, [nameof(WalkInQueueEntry.DoctorId)], unique: false);
        AssertIndex(queue,
            [nameof(WalkInQueueEntry.DepartmentId), nameof(WalkInQueueEntry.QueueDate), nameof(WalkInQueueEntry.Status), nameof(WalkInQueueEntry.Priority), nameof(WalkInQueueEntry.RegisteredAt)],
            unique: false);
        AssertIndex(queue, [nameof(WalkInQueueEntry.QueueDate), nameof(WalkInQueueEntry.RegisteredAt)], unique: false);

        var schedule = context.Model.FindEntityType(typeof(DoctorSchedule))!;
        AssertIndex(schedule, ["DoctorId", nameof(DoctorSchedule.DayOfWeek), nameof(DoctorSchedule.StartTime)], unique: false);

        Assert.DoesNotContain(patient.GetIndexes(), index =>
            index.Properties.Select(property => property.Name).SequenceEqual([nameof(Patient.Phone)]) && index.IsUnique);
        Assert.DoesNotContain(queue.GetIndexes(), index =>
            index.Properties.Select(property => property.Name).SequenceEqual([nameof(WalkInQueueEntry.QueueNumber)]) && index.IsUnique);
    }

    [Fact]
    public void RowVersion_is_configured_only_for_opted_in_entities()
    {
        using var context = CreateContext();

        foreach (var type in new[] { typeof(Patient), typeof(Appointment), typeof(WalkInQueueEntry) })
        {
            var property = context.Model.FindEntityType(type)!.FindProperty("RowVersion")!;
            Assert.True(property.IsConcurrencyToken);
            Assert.Equal(ValueGenerated.OnAddOrUpdate, property.ValueGenerated);
            Assert.Equal("rowversion", property.GetColumnType());
        }

        foreach (var type in new[] { typeof(Department), typeof(Doctor), typeof(DoctorSchedule) })
        {
            Assert.Null(context.Model.FindEntityType(type)!.FindProperty("RowVersion"));
        }
    }

    [Fact]
    public void No_unexpected_shadow_foreign_keys_or_duplicate_relationships_exist()
    {
        using var context = CreateContext();

        foreach (var entity in context.Model.GetEntityTypes())
        {
            var indexSignatures = entity.GetIndexes()
                .Select(index => string.Join("|", index.Properties.Select(property => property.Name)))
                .ToArray();
            Assert.Equal(indexSignatures.Length, indexSignatures.Distinct(StringComparer.Ordinal).Count());

            foreach (var foreignKey in entity.GetForeignKeys())
            {
                var propertyNames = foreignKey.Properties.Select(property => property.Name).ToArray();
                Assert.DoesNotContain(propertyNames, name => name.EndsWith("1", StringComparison.Ordinal));
                Assert.DoesNotContain(propertyNames, name => name.Contains("Id1", StringComparison.Ordinal));
            }
        }

        var scheduleForeignKey = context.Model.FindEntityType(typeof(DoctorSchedule))!
            .GetForeignKeys()
            .Single(foreignKey => foreignKey.PrincipalEntityType.ClrType == typeof(Doctor));
        Assert.Equal(["DoctorId"], scheduleForeignKey.Properties.Select(property => property.Name));
        Assert.NotNull(scheduleForeignKey.PrincipalToDependent);
    }

    [Fact]
    public void Patient_parameterless_constructor_is_not_public()
    {
        Assert.DoesNotContain(
            typeof(Patient).GetConstructors(BindingFlags.Public | BindingFlags.Instance),
            constructor => constructor.GetParameters().Length == 0);
    }

    private static ElsheiekhHmsDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ElsheiekhHmsDbContext>()
            .UseSqlServer("Server=unused;Database=ShapeOnly;")
            .Options;

        return new ElsheiekhHmsDbContext(options);
    }

    private static void AssertRelationship<TPrincipal, TDependent>(
        ElsheiekhHmsDbContext context,
        string foreignKeyName,
        bool required)
    {
        var dependent = context.Model.FindEntityType(typeof(TDependent))!;
        var foreignKey = dependent.GetForeignKeys().Single(fk =>
            fk.PrincipalEntityType.ClrType == typeof(TPrincipal) &&
            fk.Properties.Select(property => property.Name).SequenceEqual([foreignKeyName]));

        Assert.Equal(required, foreignKey.IsRequired);
        Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior);
    }

    private static void AssertIndex(
        Microsoft.EntityFrameworkCore.Metadata.IReadOnlyEntityType entity,
        string[] propertyNames,
        bool unique,
        string? filter = null)
    {
        var index = entity.GetIndexes().Single(index =>
            index.Properties.Select(property => property.Name).SequenceEqual(propertyNames));

        Assert.Equal(unique, index.IsUnique);
        Assert.Equal(filter, index.GetFilter());
    }
}
