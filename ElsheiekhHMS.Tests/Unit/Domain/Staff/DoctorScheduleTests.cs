using ElsheiekhHMS.Core.Domain.Staff.Entities;
using ElsheiekhHMS.Core.Exceptions;
using Xunit;

namespace ElsheiekhHMS.Tests.Unit.Domain.Staff;

public class DoctorScheduleTests
{
    private static readonly DateTimeOffset CreatedAt =
        new(2026, 9, 20, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void New_doctor_has_no_schedules()
    {
        Assert.Empty(CreateDoctor().Schedules);
    }

    [Fact]
    public void AddSchedule_creates_owned_active_schedule_with_audit_metadata()
    {
        var doctor = CreateDoctor();
        var scheduleCreatedAt = CreatedAt.AddHours(1);

        var schedule = doctor.AddSchedule(
            DayOfWeek.Sunday,
            new TimeOnly(9, 0),
            new TimeOnly(12, 0),
            30,
            scheduleCreatedAt,
            "admin-2");

        Assert.Same(schedule, Assert.Single(doctor.Schedules));
        Assert.Equal(DayOfWeek.Sunday, schedule.DayOfWeek);
        Assert.Equal(new TimeOnly(9, 0), schedule.StartTime);
        Assert.Equal(new TimeOnly(12, 0), schedule.EndTime);
        Assert.Equal(30, schedule.SlotDurationMinutes);
        Assert.True(schedule.IsActive);
        Assert.Equal(scheduleCreatedAt, schedule.CreatedAt);
        Assert.Equal("admin-2", schedule.CreatedBy);
        Assert.Null(schedule.UpdatedAt);
        Assert.Null(schedule.UpdatedBy);
        Assert.Equal(scheduleCreatedAt, doctor.UpdatedAt);
        Assert.Equal("admin-2", doctor.UpdatedBy);
        Assert.Throws<NotSupportedException>(() =>
            ((ICollection<DoctorSchedule>)doctor.Schedules).Add(schedule));
    }

    [Theory]
    [InlineData(9, 0, 9, 0, 30)]
    [InlineData(12, 0, 9, 0, 30)]
    [InlineData(9, 0, 12, 0, 0)]
    [InlineData(9, 0, 12, 0, -1)]
    [InlineData(9, 0, 10, 0, 61)]
    public void AddSchedule_rejects_invalid_interval_or_slot_duration(
        int startHour,
        int startMinute,
        int endHour,
        int endMinute,
        int slotDurationMinutes)
    {
        var doctor = CreateDoctor();

        Assert.Throws<DomainValidationException>(() =>
            doctor.AddSchedule(
                DayOfWeek.Sunday,
                new TimeOnly(startHour, startMinute),
                new TimeOnly(endHour, endMinute),
                slotDurationMinutes,
                CreatedAt,
                "admin-1"));

        Assert.Empty(doctor.Schedules);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(7)]
    public void AddSchedule_rejects_undefined_day_without_mutating_doctor(int day)
    {
        var doctor = CreateDoctor();
        var previousUpdate = CreatedAt.AddMinutes(30);
        doctor.ChangeDepartment(8, previousUpdate, "admin-1");

        Assert.Throws<DomainValidationException>(() =>
            doctor.AddSchedule(
                (DayOfWeek)day,
                new TimeOnly(9, 0),
                new TimeOnly(12, 0),
                30,
                CreatedAt.AddHours(1),
                "admin-2"));

        Assert.Empty(doctor.Schedules);
        Assert.Equal(previousUpdate, doctor.UpdatedAt);
        Assert.Equal("admin-1", doctor.UpdatedBy);
    }

    [Theory]
    [InlineData(8, 0, 10, 0)]
    [InlineData(10, 0, 13, 0)]
    [InlineData(8, 0, 13, 0)]
    [InlineData(9, 0, 12, 0)]
    public void AddSchedule_rejects_same_day_overlap(
        int startHour,
        int startMinute,
        int endHour,
        int endMinute)
    {
        var doctor = CreateDoctor();
        doctor.AddSchedule(
            DayOfWeek.Sunday,
            new TimeOnly(9, 0),
            new TimeOnly(12, 0),
            30,
            CreatedAt,
            "admin-1");

        Assert.Throws<BusinessRuleException>(() =>
            doctor.AddSchedule(
                DayOfWeek.Sunday,
                new TimeOnly(startHour, startMinute),
                new TimeOnly(endHour, endMinute),
                30,
                CreatedAt,
                "admin-1"));

        Assert.Single(doctor.Schedules);
    }

    [Fact]
    public void AddSchedule_accepts_adjacent_and_different_day_intervals()
    {
        var doctor = CreateDoctor();
        doctor.AddSchedule(
            DayOfWeek.Sunday,
            new TimeOnly(9, 0),
            new TimeOnly(12, 0),
            30,
            CreatedAt,
            "admin-1");

        doctor.AddSchedule(
            DayOfWeek.Sunday,
            new TimeOnly(12, 0),
            new TimeOnly(14, 0),
            30,
            CreatedAt,
            "admin-1");
        doctor.AddSchedule(
            DayOfWeek.Monday,
            new TimeOnly(9, 0),
            new TimeOnly(12, 0),
            30,
            CreatedAt,
            "admin-1");

        Assert.Equal(3, doctor.Schedules.Count);
    }

    [Fact]
    public void RetireSchedule_preserves_schedule_and_records_audit_metadata()
    {
        var doctor = CreateDoctor();
        var schedule = AddDefaultSchedule(doctor);
        var retiredAt = CreatedAt.AddHours(1);

        doctor.RetireSchedule(schedule, retiredAt, "admin-2");

        Assert.False(schedule.IsActive);
        Assert.Equal(retiredAt, schedule.UpdatedAt);
        Assert.Equal("admin-2", schedule.UpdatedBy);
        Assert.Equal(retiredAt, doctor.UpdatedAt);
        Assert.Equal("admin-2", doctor.UpdatedBy);
        Assert.Same(schedule, Assert.Single(doctor.Schedules));
    }

    [Fact]
    public void Repeated_retirement_preserves_first_retirement_metadata()
    {
        var doctor = CreateDoctor();
        var schedule = AddDefaultSchedule(doctor);
        var retiredAt = CreatedAt.AddHours(1);
        doctor.RetireSchedule(schedule, retiredAt, "admin-2");

        Assert.Throws<BusinessRuleException>(() =>
            doctor.RetireSchedule(schedule, CreatedAt.AddHours(2), "admin-3"));

        Assert.Equal(retiredAt, schedule.UpdatedAt);
        Assert.Equal("admin-2", schedule.UpdatedBy);
        Assert.Equal(retiredAt, doctor.UpdatedAt);
        Assert.Equal("admin-2", doctor.UpdatedBy);
    }

    [Fact]
    public void Doctor_cannot_retire_schedule_owned_by_another_doctor()
    {
        var owner = CreateDoctor();
        var otherDoctor = CreateDoctor("DR-002");
        var schedule = AddDefaultSchedule(owner);

        Assert.Throws<BusinessRuleException>(() =>
            otherDoctor.RetireSchedule(schedule, CreatedAt.AddHours(1), "admin-2"));

        Assert.True(schedule.IsActive);
        Assert.Null(schedule.UpdatedAt);
        Assert.Null(schedule.UpdatedBy);
        Assert.Null(otherDoctor.UpdatedAt);
        Assert.Null(otherDoctor.UpdatedBy);
    }

    [Fact]
    public void Retired_schedule_no_longer_blocks_replacement_interval()
    {
        var doctor = CreateDoctor();
        var original = AddDefaultSchedule(doctor);
        doctor.RetireSchedule(original, CreatedAt.AddHours(1), "admin-2");

        var replacement = doctor.AddSchedule(
            DayOfWeek.Sunday,
            new TimeOnly(9, 0),
            new TimeOnly(12, 0),
            30,
            CreatedAt.AddHours(2),
            "admin-3");

        Assert.Equal(2, doctor.Schedules.Count);
        Assert.False(original.IsActive);
        Assert.True(replacement.IsActive);
    }

    [Fact]
    public void On_leave_doctor_can_administer_schedules()
    {
        var doctor = CreateDoctor();
        doctor.PlaceOnLeave(CreatedAt.AddHours(1), "admin-2");

        var schedule = doctor.AddSchedule(
            DayOfWeek.Sunday,
            new TimeOnly(9, 0),
            new TimeOnly(12, 0),
            30,
            CreatedAt.AddHours(2),
            "admin-3");
        doctor.RetireSchedule(schedule, CreatedAt.AddHours(3), "admin-4");

        Assert.False(schedule.IsActive);
    }

    [Fact]
    public void Inactive_doctor_rejects_schedule_addition_and_retirement()
    {
        var doctor = CreateDoctor();
        var schedule = AddDefaultSchedule(doctor);
        var deactivatedAt = CreatedAt.AddHours(1);
        doctor.Deactivate(deactivatedAt, "admin-2");

        Assert.Throws<BusinessRuleException>(() =>
            doctor.AddSchedule(
                DayOfWeek.Monday,
                new TimeOnly(9, 0),
                new TimeOnly(12, 0),
                30,
                CreatedAt.AddHours(2),
                "admin-3"));
        Assert.Throws<BusinessRuleException>(() =>
            doctor.RetireSchedule(schedule, CreatedAt.AddHours(2), "admin-3"));

        Assert.True(schedule.IsActive);
        Assert.Equal(deactivatedAt, doctor.UpdatedAt);
        Assert.Equal("admin-2", doctor.UpdatedBy);
    }

    [Fact]
    public void Deleted_doctor_rejects_schedule_addition_and_retirement()
    {
        var doctor = CreateDoctor();
        var schedule = AddDefaultSchedule(doctor);
        doctor.Deactivate(CreatedAt.AddHours(1), "admin-2");
        doctor.MarkDeleted(CreatedAt.AddHours(2), "admin-3");

        Assert.Throws<BusinessRuleException>(() =>
            doctor.AddSchedule(
                DayOfWeek.Monday,
                new TimeOnly(9, 0),
                new TimeOnly(12, 0),
                30,
                CreatedAt.AddHours(3),
                "admin-4"));
        Assert.Throws<BusinessRuleException>(() =>
            doctor.RetireSchedule(schedule, CreatedAt.AddHours(3), "admin-4"));

        Assert.True(schedule.IsActive);
    }

    private static DoctorSchedule AddDefaultSchedule(Doctor doctor) =>
        doctor.AddSchedule(
            DayOfWeek.Sunday,
            new TimeOnly(9, 0),
            new TimeOnly(12, 0),
            30,
            CreatedAt,
            "admin-1");

    private static Doctor CreateDoctor(string doctorCode = "DR-001") =>
        new(
            doctorCode,
            "Dr. Sara Ahmed",
            "Cardiology",
            false,
            150m,
            7,
            CreatedAt,
            "admin-1");
}
