using ElsheiekhHMS.Core.Common;
using ElsheiekhHMS.Core.Domain.Staff.Enums;
using ElsheiekhHMS.Core.Exceptions;
using System.Collections.ObjectModel;

namespace ElsheiekhHMS.Core.Domain.Staff.Entities;

public sealed class Doctor : SoftDeletableEntity
{
    private readonly List<DoctorSchedule> _schedules = [];
    private readonly ReadOnlyCollection<DoctorSchedule> _readOnlySchedules;

    public Doctor(
        string doctorCode,
        string fullName,
        string? specialization,
        bool isGeneralPractitioner,
        decimal consultationFee,
        int departmentId,
        DateTimeOffset createdAt,
        string? createdBy)
    {
        _readOnlySchedules = _schedules.AsReadOnly();
        DoctorCode = NormalizeRequired(doctorCode, "Doctor code");
        FullName = NormalizeRequired(fullName, "Doctor name");
        Specialization = NormalizeSpecialization(specialization, isGeneralPractitioner);
        ValidateConsultationFee(consultationFee);
        ValidateDepartmentId(departmentId);

        IsGeneralPractitioner = isGeneralPractitioner;
        ConsultationFee = consultationFee;
        DepartmentId = departmentId;
        Status = DoctorStatus.Active;
        CreatedAt = createdAt;
        CreatedBy = createdBy;
    }

    public string DoctorCode { get; private set; }

    public string FullName { get; private set; }

    public string? Specialization { get; private set; }

    public bool IsGeneralPractitioner { get; private set; }

    public decimal ConsultationFee { get; private set; }

    public DoctorStatus Status { get; private set; }

    public int DepartmentId { get; private set; }

    public IReadOnlyCollection<DoctorSchedule> Schedules => _readOnlySchedules;

    public void UpdateProfessionalDetails(
        string fullName,
        string? specialization,
        bool isGeneralPractitioner,
        decimal consultationFee,
        DateTimeOffset updatedAt,
        string? updatedBy)
    {
        EnsureMutable();

        var normalizedName = NormalizeRequired(fullName, "Doctor name");
        var normalizedSpecialization =
            NormalizeSpecialization(specialization, isGeneralPractitioner);
        ValidateConsultationFee(consultationFee);

        FullName = normalizedName;
        Specialization = normalizedSpecialization;
        IsGeneralPractitioner = isGeneralPractitioner;
        ConsultationFee = consultationFee;
        UpdatedAt = updatedAt;
        UpdatedBy = updatedBy;
    }

    public void ChangeDepartment(
        int departmentId,
        DateTimeOffset updatedAt,
        string? updatedBy)
    {
        EnsureMutable();
        ValidateDepartmentId(departmentId);

        DepartmentId = departmentId;
        UpdatedAt = updatedAt;
        UpdatedBy = updatedBy;
    }

    public void PlaceOnLeave(DateTimeOffset updatedAt, string? updatedBy)
    {
        EnsureNotDeleted();
        if (Status != DoctorStatus.Active)
        {
            throw new BusinessRuleException("Only an active doctor can be placed on leave.");
        }

        SetStatus(DoctorStatus.OnLeave, updatedAt, updatedBy);
    }

    public void ReturnToActive(DateTimeOffset updatedAt, string? updatedBy)
    {
        EnsureNotDeleted();
        if (Status != DoctorStatus.OnLeave)
        {
            throw new BusinessRuleException("Only a doctor on leave can return to active status.");
        }

        SetStatus(DoctorStatus.Active, updatedAt, updatedBy);
    }

    public void Deactivate(DateTimeOffset updatedAt, string? updatedBy)
    {
        EnsureNotDeleted();
        if (Status == DoctorStatus.Inactive)
        {
            throw new BusinessRuleException("The doctor is already inactive.");
        }

        SetStatus(DoctorStatus.Inactive, updatedAt, updatedBy);
    }

    public void MarkDeleted(DateTimeOffset deletedAt, string? deletedBy)
    {
        EnsureNotDeleted();
        if (Status != DoctorStatus.Inactive)
        {
            throw new BusinessRuleException("Only an inactive doctor can be deleted.");
        }

        IsDeleted = true;
        DeletedAt = deletedAt;
        DeletedBy = deletedBy;
        UpdatedAt = deletedAt;
        UpdatedBy = deletedBy;
    }

    public DoctorSchedule AddSchedule(
        DayOfWeek dayOfWeek,
        TimeOnly startTime,
        TimeOnly endTime,
        int slotDurationMinutes,
        DateTimeOffset createdAt,
        string? createdBy)
    {
        EnsureMutable();
        var schedule = new DoctorSchedule(
            dayOfWeek,
            startTime,
            endTime,
            slotDurationMinutes,
            createdAt,
            createdBy);

        if (_schedules.Any(existing =>
                existing.IsActive &&
                existing.DayOfWeek == dayOfWeek &&
                startTime < existing.EndTime &&
                endTime > existing.StartTime))
        {
            throw new BusinessRuleException(
                "The schedule overlaps another active interval for this doctor.");
        }

        _schedules.Add(schedule);
        UpdatedAt = createdAt;
        UpdatedBy = createdBy;
        return schedule;
    }

    public void RetireSchedule(
        DoctorSchedule schedule,
        DateTimeOffset updatedAt,
        string? updatedBy)
    {
        EnsureMutable();
        if (!_schedules.Contains(schedule))
        {
            throw new BusinessRuleException("The schedule does not belong to this doctor.");
        }

        schedule.Retire(updatedAt, updatedBy);
        UpdatedAt = updatedAt;
        UpdatedBy = updatedBy;
    }

    private void SetStatus(
        DoctorStatus status,
        DateTimeOffset updatedAt,
        string? updatedBy)
    {
        Status = status;
        UpdatedAt = updatedAt;
        UpdatedBy = updatedBy;
    }

    private void EnsureMutable()
    {
        EnsureNotDeleted();
        if (Status == DoctorStatus.Inactive)
        {
            throw new BusinessRuleException("Inactive doctors cannot be changed.");
        }
    }

    private void EnsureNotDeleted()
    {
        if (IsDeleted)
        {
            throw new BusinessRuleException("Deleted doctors cannot be changed.");
        }
    }

    private static string NormalizeRequired(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainValidationException($"{fieldName} is required.");
        }

        return value.Trim();
    }

    private static string? NormalizeSpecialization(
        string? specialization,
        bool isGeneralPractitioner)
    {
        if (string.IsNullOrWhiteSpace(specialization))
        {
            if (!isGeneralPractitioner)
            {
                throw new DomainValidationException(
                    "Specialization is required for a non-GP doctor.");
            }

            return null;
        }

        return specialization.Trim();
    }

    private static void ValidateConsultationFee(decimal consultationFee)
    {
        if (consultationFee < 0)
        {
            throw new DomainValidationException("Consultation fee cannot be negative.");
        }
    }

    private static void ValidateDepartmentId(int departmentId)
    {
        if (departmentId <= 0)
        {
            throw new DomainValidationException("Department identity must be positive.");
        }
    }
}
