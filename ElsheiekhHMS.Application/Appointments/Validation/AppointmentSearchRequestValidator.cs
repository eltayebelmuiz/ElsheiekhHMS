using ElsheiekhHMS.Application.Appointments.Contracts;
using ElsheiekhHMS.Application.Common.Validation;

namespace ElsheiekhHMS.Application.Appointments.Validation;

public sealed class AppointmentSearchRequestValidator : IRequestValidator<AppointmentSearchRequest>
{
    private readonly PageRequestValidator _pageValidator = new();

    public ValidationResult Validate(AppointmentSearchRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var errors = new List<ValidationError>();
        if (request.FromDate == DateOnly.MinValue) errors.Add(new(nameof(request.FromDate), "appointment.from_date.required", "From date is required."));
        if (request.ToDate == DateOnly.MinValue) errors.Add(new(nameof(request.ToDate), "appointment.to_date.required", "To date is required."));
        if (request.FromDate != DateOnly.MinValue && request.ToDate != DateOnly.MinValue)
        {
            if (request.ToDate < request.FromDate) errors.Add(new(nameof(request.ToDate), "appointment.date_range.order", "To date must not precede from date."));
            else if (request.ToDate.DayNumber - request.FromDate.DayNumber > 31) errors.Add(new(nameof(request.ToDate), "appointment.date_range.maximum", "Appointment date range cannot exceed 31 days."));
        }
        PositiveIfPresent(errors, request.PatientId, nameof(request.PatientId));
        PositiveIfPresent(errors, request.DoctorId, nameof(request.DoctorId));
        PositiveIfPresent(errors, request.DepartmentId, nameof(request.DepartmentId));
        if (request.Status.HasValue) EnumValue(errors, request.Status.Value, nameof(request.Status));
        errors.AddRange(_pageValidator.Validate(request.Page).Errors);
        EnumValue(errors, request.SortBy, nameof(request.SortBy));
        EnumValue(errors, request.SortDirection, nameof(request.SortDirection));
        return new ValidationResult(errors);
    }

    private static void PositiveIfPresent(List<ValidationError> errors, int? value, string field) {
        if (value.HasValue && value.Value <= 0) errors.Add(new(field, $"appointment.{field}.positive", $"{field} must be positive when supplied."));
    }

    private static void EnumValue<T>(List<ValidationError> errors, T value, string field) where T : struct, Enum {
        if (!Enum.IsDefined(value)) errors.Add(new(field, $"appointment.{field}.invalid", $"{field} is invalid."));
    }
}
