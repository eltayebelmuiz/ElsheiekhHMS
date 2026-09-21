using ElsheiekhHMS.Application.Appointments.Contracts;
using ElsheiekhHMS.Application.Common.Validation;

namespace ElsheiekhHMS.Application.Appointments.Validation;

public sealed class CreateAppointmentRequestValidator : IRequestValidator<CreateAppointmentRequest>
{
    public ValidationResult Validate(CreateAppointmentRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var errors = new List<ValidationError>();
        Positive(errors, request.PatientId, nameof(request.PatientId));
        Positive(errors, request.DoctorId, nameof(request.DoctorId));
        Positive(errors, request.DepartmentId, nameof(request.DepartmentId));
        if (request.ScheduledDate == DateOnly.MinValue) errors.Add(new(nameof(request.ScheduledDate), "appointment.scheduled_date.required", "Scheduled date is required."));
        EnumValue(errors, request.Type, nameof(request.Type));
        Optional(errors, request.Notes, nameof(request.Notes), 2000);
        return new ValidationResult(errors);
    }

    private static void Positive(List<ValidationError> errors, int value, string field) {
        if (value <= 0) errors.Add(new(field, $"appointment.{field}.positive", $"{field} must be positive."));
    }

    private static void Optional(List<ValidationError> errors, string? value, string field, int max) {
        if (value is not null && value.Length > max) errors.Add(new(field, $"appointment.{field}.maximum", $"{field} cannot exceed {max} characters."));
    }

    private static void EnumValue<T>(List<ValidationError> errors, T value, string field) where T : struct, Enum {
        if (!Enum.IsDefined(value)) errors.Add(new(field, $"appointment.{field}.invalid", $"{field} is invalid."));
    }
}
