using ElsheiekhHMS.Core.Domain.Scheduling.Entities;
using ElsheiekhHMS.Core.Domain.Scheduling.Enums;
using ElsheiekhHMS.Core.Exceptions;
using ElsheiekhHMS.Core.Interfaces;

namespace ElsheiekhHMS.Tests.Unit.Domain.Scheduling;

public class AppointmentTests
{
    private static readonly DateOnly ScheduledDate = new(2026, 9, 21);
    private static readonly DateTimeOffset CreatedAt =
        new(2026, 9, 21, 7, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset UpdatedAt =
        new(2026, 9, 21, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Construction_preserves_local_booking_references_and_audit_data()
    {
        var appointment = CreateAppointment(notes: "  routine review  ");

        Assert.Equal("AP-41", appointment.AppointmentCode);
        Assert.Equal(41, appointment.PatientId);
        Assert.Equal(17, appointment.DoctorId);
        Assert.Equal(3, appointment.DepartmentId);
        Assert.Equal(ScheduledDate, appointment.ScheduledDate);
        Assert.Equal(new TimeOnly(9, 30), appointment.ScheduledTime);
        Assert.Equal(AppointmentType.General, appointment.Type);
        Assert.Equal(AppointmentStatus.Scheduled, appointment.Status);
        Assert.Equal("routine review", appointment.Notes);
        Assert.Equal(CreatedAt, appointment.CreatedAt);
        Assert.Equal("desk-1", appointment.CreatedBy);
        Assert.False(appointment.IsDeleted);
        Assert.IsAssignableFrom<IHasConcurrencyToken>(appointment);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Appointment_code_is_required(string? code)
    {
        Assert.Throws<DomainValidationException>(() => CreateAppointment(code: code!));
    }

    [Theory]
    [InlineData(0, 17, 3)]
    [InlineData(41, 0, 3)]
    [InlineData(41, 17, 0)]
    public void Patient_doctor_and_department_ids_are_required(
        int patientId, int doctorId, int departmentId)
    {
        Assert.Throws<DomainValidationException>(() =>
            CreateAppointment(patientId: patientId, doctorId: doctorId, departmentId: departmentId));
    }

    [Fact]
    public void Default_date_and_undefined_type_are_rejected()
    {
        Assert.Throws<DomainValidationException>(() =>
            CreateAppointment(scheduledDate: DateOnly.MinValue));
        Assert.Throws<DomainValidationException>(() =>
            CreateAppointment(type: (AppointmentType)100));
    }

    [Theory]
    [InlineData(AppointmentType.General)]
    [InlineData(AppointmentType.Specialist)]
    [InlineData(AppointmentType.Emergency)]
    [InlineData(AppointmentType.FollowUp)]
    [InlineData(AppointmentType.LabTest)]
    [InlineData(AppointmentType.Radiology)]
    public void Every_approved_appointment_type_can_be_stored(AppointmentType type)
    {
        var appointment = CreateAppointment(type: type);

        Assert.Equal(type, appointment.Type);
    }

    [Fact]
    public void Non_utc_creation_time_is_rejected()
    {
        Assert.Throws<DomainValidationException>(() =>
            CreateAppointment(createdAt: new DateTimeOffset(2026, 9, 21, 7, 0, 0,
                TimeSpan.FromHours(2))));
    }

    [Fact]
    public void A_structurally_valid_past_local_booking_is_allowed_for_application_policy()
    {
        var appointment = CreateAppointment(scheduledDate: new DateOnly(2020, 1, 1));

        Assert.Equal(new DateOnly(2020, 1, 1), appointment.ScheduledDate);
        Assert.Equal(AppointmentStatus.Scheduled, appointment.Status);
    }

    [Fact]
    public void Scheduled_appointment_can_be_confirmed_checked_in_and_completed()
    {
        var appointment = CreateAppointment();
        appointment.Confirm(UpdatedAt, "desk-1");
        appointment.CheckIn(UpdatedAt.AddMinutes(5), "desk-1");
        appointment.Complete(UpdatedAt.AddMinutes(25), "doctor-1");

        Assert.Equal(AppointmentStatus.Completed, appointment.Status);
        Assert.Equal(3, appointment.DepartmentId);
        Assert.Equal(UpdatedAt.AddMinutes(25), appointment.UpdatedAt);
        Assert.Equal("doctor-1", appointment.UpdatedBy);
        Assert.False(appointment.IsDeleted);
    }

    [Fact]
    public void Scheduled_appointment_can_check_in_without_confirmation()
    {
        var appointment = CreateAppointment();
        appointment.CheckIn(UpdatedAt, "desk-1");

        Assert.Equal(AppointmentStatus.CheckedIn, appointment.Status);
    }

    [Fact]
    public void Cancellation_preserves_record_and_reason()
    {
        var appointment = CreateAppointment();
        appointment.Cancel("  patient requested  ", UpdatedAt, "desk-1");

        Assert.Equal(AppointmentStatus.Cancelled, appointment.Status);
        Assert.Equal("patient requested", appointment.CancellationReason);
        Assert.Equal(UpdatedAt, appointment.CancelledAt);
        Assert.False(appointment.IsDeleted);
    }

    [Fact]
    public void Scheduled_appointment_can_be_marked_no_show()
    {
        var appointment = CreateAppointment();
        appointment.MarkNoShow(UpdatedAt, "desk-1");

        Assert.Equal(AppointmentStatus.NoShow, appointment.Status);
        Assert.False(appointment.IsDeleted);
    }

    [Fact]
    public void Invalid_and_terminal_transitions_do_not_change_state()
    {
        var appointment = CreateAppointment();
        Assert.Throws<BusinessRuleException>(() => appointment.Complete(UpdatedAt, "desk-1"));
        Assert.Equal(AppointmentStatus.Scheduled, appointment.Status);

        appointment.CheckIn(UpdatedAt, "desk-1");
        Assert.Throws<BusinessRuleException>(() => appointment.Cancel("late", UpdatedAt, "desk-1"));
        appointment.Complete(UpdatedAt.AddMinutes(20), "doctor-1");
        Assert.Throws<BusinessRuleException>(() => appointment.Confirm(UpdatedAt, "desk-1"));
        Assert.Equal(AppointmentStatus.Completed, appointment.Status);
    }

    [Fact]
    public void Non_utc_lifecycle_time_is_rejected_before_mutation()
    {
        var appointment = CreateAppointment();
        var localTime = new DateTimeOffset(2026, 9, 21, 8, 0, 0, TimeSpan.FromHours(2));

        Assert.Throws<DomainValidationException>(() => appointment.Confirm(localTime, "desk-1"));
        Assert.Equal(AppointmentStatus.Scheduled, appointment.Status);
        Assert.Null(appointment.UpdatedAt);
    }

    private static Appointment CreateAppointment(
        string code = "AP-41",
        int patientId = 41,
        int doctorId = 17,
        int departmentId = 3,
        DateOnly? scheduledDate = null,
        AppointmentType type = AppointmentType.General,
        string? notes = null,
        DateTimeOffset? createdAt = null) =>
        new(code, patientId, doctorId, departmentId, scheduledDate ?? ScheduledDate,
            new TimeOnly(9, 30), type, notes, createdAt ?? CreatedAt, "desk-1");
}
