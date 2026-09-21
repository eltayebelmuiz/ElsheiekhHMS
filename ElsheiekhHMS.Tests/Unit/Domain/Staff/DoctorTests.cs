using ElsheiekhHMS.Core.Domain.Staff.Entities;
using ElsheiekhHMS.Core.Domain.Staff.Enums;
using ElsheiekhHMS.Core.Exceptions;
using Xunit;

namespace ElsheiekhHMS.Tests.Unit.Domain.Staff;

public class DoctorTests
{
    private static readonly DateTimeOffset CreatedAt =
        new(2026, 9, 20, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Constructor_creates_active_doctor_with_normalized_values_and_audit_metadata()
    {
        var doctor = new Doctor(
            "  DR-001  ",
            "  Dr. Sara Ahmed  ",
            "  Cardiology  ",
            false,
            150.500m,
            7,
            CreatedAt,
            "admin-1");

        Assert.Equal("DR-001", doctor.DoctorCode);
        Assert.Equal("Dr. Sara Ahmed", doctor.FullName);
        Assert.Equal("Cardiology", doctor.Specialization);
        Assert.False(doctor.IsGeneralPractitioner);
        Assert.Equal(150.500m, doctor.ConsultationFee);
        Assert.Equal(DoctorStatus.Active, doctor.Status);
        Assert.Equal(7, doctor.DepartmentId);
        Assert.False(doctor.IsDeleted);
        Assert.Null(doctor.DeletedAt);
        Assert.Null(doctor.DeletedBy);
        Assert.Equal(CreatedAt, doctor.CreatedAt);
        Assert.Equal("admin-1", doctor.CreatedBy);
        Assert.Null(doctor.UpdatedAt);
        Assert.Null(doctor.UpdatedBy);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_rejects_missing_doctor_code(string? doctorCode)
    {
        Assert.Throws<DomainValidationException>(() =>
            CreateDoctor(doctorCode: doctorCode!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_rejects_missing_full_name(string? fullName)
    {
        Assert.Throws<DomainValidationException>(() =>
            CreateDoctor(fullName: fullName!));
    }

    [Fact]
    public void Constructor_requires_specialization_for_non_gp_doctor()
    {
        Assert.Throws<DomainValidationException>(() =>
            CreateDoctor(specialization: " ", isGeneralPractitioner: false));
    }

    [Fact]
    public void Constructor_allows_gp_without_specialization()
    {
        var doctor = CreateDoctor(specialization: null, isGeneralPractitioner: true);

        Assert.Null(doctor.Specialization);
        Assert.True(doctor.IsGeneralPractitioner);
    }

    [Fact]
    public void Constructor_rejects_negative_consultation_fee()
    {
        Assert.Throws<DomainValidationException>(() =>
            CreateDoctor(consultationFee: -0.001m));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_rejects_nonpositive_department_id(int departmentId)
    {
        Assert.Throws<DomainValidationException>(() =>
            CreateDoctor(departmentId: departmentId));
    }

    [Fact]
    public void UpdateProfessionalDetails_changes_values_and_records_audit_metadata()
    {
        var doctor = CreateDoctor();
        var updatedAt = CreatedAt.AddHours(1);

        doctor.UpdateProfessionalDetails(
            "  Dr. Sara Hassan  ", null, true, 175m, updatedAt, "admin-2");

        Assert.Equal("Dr. Sara Hassan", doctor.FullName);
        Assert.Null(doctor.Specialization);
        Assert.True(doctor.IsGeneralPractitioner);
        Assert.Equal(175m, doctor.ConsultationFee);
        Assert.Equal(updatedAt, doctor.UpdatedAt);
        Assert.Equal("admin-2", doctor.UpdatedBy);
    }

    [Fact]
    public void Invalid_professional_update_preserves_values_and_audit_metadata()
    {
        var doctor = CreateDoctor();
        var previousUpdate = CreatedAt.AddMinutes(30);
        doctor.ChangeDepartment(8, previousUpdate, "admin-1");

        Assert.Throws<DomainValidationException>(() =>
            doctor.UpdateProfessionalDetails(
                "Changed", null, false, 999m, CreatedAt.AddHours(1), "admin-2"));

        Assert.Equal("Dr. Sara Ahmed", doctor.FullName);
        Assert.Equal("Cardiology", doctor.Specialization);
        Assert.False(doctor.IsGeneralPractitioner);
        Assert.Equal(150m, doctor.ConsultationFee);
        Assert.Equal(8, doctor.DepartmentId);
        Assert.Equal(previousUpdate, doctor.UpdatedAt);
        Assert.Equal("admin-1", doctor.UpdatedBy);
    }

    [Fact]
    public void ChangeDepartment_changes_department_and_records_audit_metadata()
    {
        var doctor = CreateDoctor();
        var updatedAt = CreatedAt.AddHours(1);

        doctor.ChangeDepartment(8, updatedAt, "admin-2");

        Assert.Equal(8, doctor.DepartmentId);
        Assert.Equal(updatedAt, doctor.UpdatedAt);
        Assert.Equal("admin-2", doctor.UpdatedBy);
    }

    [Fact]
    public void Invalid_department_change_preserves_department_and_audit_metadata()
    {
        var doctor = CreateDoctor();
        var previousUpdate = CreatedAt.AddMinutes(30);
        doctor.ChangeDepartment(8, previousUpdate, "admin-1");

        Assert.Throws<DomainValidationException>(() =>
            doctor.ChangeDepartment(0, CreatedAt.AddHours(1), "admin-2"));

        Assert.Equal(8, doctor.DepartmentId);
        Assert.Equal(previousUpdate, doctor.UpdatedAt);
        Assert.Equal("admin-1", doctor.UpdatedBy);
    }

    [Fact]
    public void PlaceOnLeave_changes_active_doctor_and_records_audit_metadata()
    {
        var doctor = CreateDoctor();
        var updatedAt = CreatedAt.AddHours(1);

        doctor.PlaceOnLeave(updatedAt, "admin-2");

        Assert.Equal(DoctorStatus.OnLeave, doctor.Status);
        Assert.Equal(updatedAt, doctor.UpdatedAt);
        Assert.Equal("admin-2", doctor.UpdatedBy);
    }

    [Fact]
    public void ReturnToActive_changes_on_leave_doctor_and_records_audit_metadata()
    {
        var doctor = CreateDoctor();
        doctor.PlaceOnLeave(CreatedAt.AddHours(1), "admin-2");
        var returnedAt = CreatedAt.AddHours(2);

        doctor.ReturnToActive(returnedAt, "admin-3");

        Assert.Equal(DoctorStatus.Active, doctor.Status);
        Assert.Equal(returnedAt, doctor.UpdatedAt);
        Assert.Equal("admin-3", doctor.UpdatedBy);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Deactivate_accepts_active_or_on_leave_doctor(bool placeOnLeave)
    {
        var doctor = CreateDoctor();
        if (placeOnLeave)
        {
            doctor.PlaceOnLeave(CreatedAt.AddMinutes(30), "admin-1");
        }

        var deactivatedAt = CreatedAt.AddHours(1);
        doctor.Deactivate(deactivatedAt, "admin-2");

        Assert.Equal(DoctorStatus.Inactive, doctor.Status);
        Assert.Equal(deactivatedAt, doctor.UpdatedAt);
        Assert.Equal("admin-2", doctor.UpdatedBy);
    }

    [Fact]
    public void Invalid_status_transition_preserves_status_and_audit_metadata()
    {
        var doctor = CreateDoctor();
        var leaveAt = CreatedAt.AddHours(1);
        doctor.PlaceOnLeave(leaveAt, "admin-2");

        Assert.Throws<BusinessRuleException>(() =>
            doctor.PlaceOnLeave(CreatedAt.AddHours(2), "admin-3"));

        Assert.Equal(DoctorStatus.OnLeave, doctor.Status);
        Assert.Equal(leaveAt, doctor.UpdatedAt);
        Assert.Equal("admin-2", doctor.UpdatedBy);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MarkDeleted_rejects_active_or_on_leave_doctor(bool placeOnLeave)
    {
        var doctor = CreateDoctor();
        if (placeOnLeave)
        {
            doctor.PlaceOnLeave(CreatedAt.AddHours(1), "admin-2");
        }

        Assert.Throws<BusinessRuleException>(() =>
            doctor.MarkDeleted(CreatedAt.AddHours(2), "admin-3"));

        Assert.False(doctor.IsDeleted);
        Assert.Null(doctor.DeletedAt);
        Assert.Null(doctor.DeletedBy);
    }

    [Fact]
    public void MarkDeleted_soft_deletes_inactive_doctor_with_consistent_metadata()
    {
        var doctor = CreateDoctor();
        doctor.Deactivate(CreatedAt.AddHours(1), "admin-2");
        var deletedAt = CreatedAt.AddHours(2);

        doctor.MarkDeleted(deletedAt, "admin-3");

        Assert.True(doctor.IsDeleted);
        Assert.Equal(deletedAt, doctor.DeletedAt);
        Assert.Equal("admin-3", doctor.DeletedBy);
        Assert.Equal(deletedAt, doctor.UpdatedAt);
        Assert.Equal("admin-3", doctor.UpdatedBy);
    }

    [Fact]
    public void Repeated_deletion_preserves_first_deletion_metadata()
    {
        var doctor = CreateDoctor();
        doctor.Deactivate(CreatedAt.AddHours(1), "admin-2");
        var deletedAt = CreatedAt.AddHours(2);
        doctor.MarkDeleted(deletedAt, "admin-3");

        Assert.Throws<BusinessRuleException>(() =>
            doctor.MarkDeleted(CreatedAt.AddHours(3), "admin-4"));

        Assert.Equal(deletedAt, doctor.DeletedAt);
        Assert.Equal("admin-3", doctor.DeletedBy);
        Assert.Equal(deletedAt, doctor.UpdatedAt);
        Assert.Equal("admin-3", doctor.UpdatedBy);
    }

    [Fact]
    public void Inactive_doctor_rejects_professional_department_and_lifecycle_changes()
    {
        var doctor = CreateDoctor();
        var deactivatedAt = CreatedAt.AddHours(1);
        doctor.Deactivate(deactivatedAt, "admin-2");

        Assert.Throws<BusinessRuleException>(() =>
            doctor.UpdateProfessionalDetails(
                "Changed", "Neurology", false, 200m, CreatedAt.AddHours(2), "admin-3"));
        Assert.Throws<BusinessRuleException>(() =>
            doctor.ChangeDepartment(8, CreatedAt.AddHours(2), "admin-3"));
        Assert.Throws<BusinessRuleException>(() =>
            doctor.PlaceOnLeave(CreatedAt.AddHours(2), "admin-3"));
        Assert.Throws<BusinessRuleException>(() =>
            doctor.ReturnToActive(CreatedAt.AddHours(2), "admin-3"));
        Assert.Throws<BusinessRuleException>(() =>
            doctor.Deactivate(CreatedAt.AddHours(2), "admin-3"));

        Assert.Equal("Dr. Sara Ahmed", doctor.FullName);
        Assert.Equal(7, doctor.DepartmentId);
        Assert.Equal(DoctorStatus.Inactive, doctor.Status);
        Assert.Equal(deactivatedAt, doctor.UpdatedAt);
        Assert.Equal("admin-2", doctor.UpdatedBy);
    }

    [Fact]
    public void Deleted_doctor_rejects_all_lifecycle_changes()
    {
        var doctor = CreateDoctor();
        doctor.Deactivate(CreatedAt.AddHours(1), "admin-2");
        var deletedAt = CreatedAt.AddHours(2);
        doctor.MarkDeleted(deletedAt, "admin-3");

        Assert.Throws<BusinessRuleException>(() =>
            doctor.UpdateProfessionalDetails(
                "Changed", "Neurology", false, 200m, CreatedAt.AddHours(3), "admin-4"));
        Assert.Throws<BusinessRuleException>(() =>
            doctor.ChangeDepartment(8, CreatedAt.AddHours(3), "admin-4"));
        Assert.Throws<BusinessRuleException>(() =>
            doctor.PlaceOnLeave(CreatedAt.AddHours(3), "admin-4"));
        Assert.Throws<BusinessRuleException>(() =>
            doctor.ReturnToActive(CreatedAt.AddHours(3), "admin-4"));
        Assert.Throws<BusinessRuleException>(() =>
            doctor.Deactivate(CreatedAt.AddHours(3), "admin-4"));

        Assert.Equal(deletedAt, doctor.UpdatedAt);
        Assert.Equal("admin-3", doctor.UpdatedBy);
    }

    private static Doctor CreateDoctor(
        string doctorCode = "DR-001",
        string fullName = "Dr. Sara Ahmed",
        string? specialization = "Cardiology",
        bool isGeneralPractitioner = false,
        decimal consultationFee = 150m,
        int departmentId = 7) =>
        new(
            doctorCode,
            fullName,
            specialization,
            isGeneralPractitioner,
            consultationFee,
            departmentId,
            CreatedAt,
            "admin-1");
}
