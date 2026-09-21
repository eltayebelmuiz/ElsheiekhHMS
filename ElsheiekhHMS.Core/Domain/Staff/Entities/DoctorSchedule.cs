using ElsheiekhHMS.Core.Common;
using ElsheiekhHMS.Core.Exceptions;

namespace ElsheiekhHMS.Core.Domain.Staff.Entities;

public sealed class DoctorSchedule : AuditableEntity
{
    internal DoctorSchedule(
        DayOfWeek dayOfWeek,
        TimeOnly startTime,
        TimeOnly endTime,
        int slotDurationMinutes,
        DateTimeOffset createdAt,
        string? createdBy)
    {
        if (!Enum.IsDefined(dayOfWeek))
        {
            throw new DomainValidationException("Schedule day must be a valid day of week.");
        }

        if (startTime >= endTime)
        {
            throw new DomainValidationException(
                "Schedule start time must be earlier than end time.");
        }

        var intervalMinutes = (endTime.ToTimeSpan() - startTime.ToTimeSpan()).TotalMinutes;
        if (slotDurationMinutes <= 0 || slotDurationMinutes > intervalMinutes)
        {
            throw new DomainValidationException(
                "Slot duration must be positive and fit within the schedule interval.");
        }

        DayOfWeek = dayOfWeek;
        StartTime = startTime;
        EndTime = endTime;
        SlotDurationMinutes = slotDurationMinutes;
        IsActive = true;
        CreatedAt = createdAt;
        CreatedBy = createdBy;
    }

    public DayOfWeek DayOfWeek { get; private set; }

    public TimeOnly StartTime { get; private set; }

    public TimeOnly EndTime { get; private set; }

    public int SlotDurationMinutes { get; private set; }

    public bool IsActive { get; private set; }

    internal void Retire(DateTimeOffset updatedAt, string? updatedBy)
    {
        if (!IsActive)
        {
            throw new BusinessRuleException("The schedule is already retired.");
        }

        IsActive = false;
        UpdatedAt = updatedAt;
        UpdatedBy = updatedBy;
    }
}
