using ElsheiekhHMS.Application.Appointments.Contracts;
using ElsheiekhHMS.Application.Common.Validation;

namespace ElsheiekhHMS.Application.Appointments.Validation;

public sealed class AppointmentActionRequestValidator : IRequestValidator<AppointmentActionRequest>
{
    public ValidationResult Validate(AppointmentActionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var errors = new List<ValidationError>();
        if (request.AppointmentId <= 0)
        {
            errors.Add(new(nameof(request.AppointmentId), "appointment.id.positive", "Appointment ID must be positive."));
        }

        return new ValidationResult(errors);
    }
}
