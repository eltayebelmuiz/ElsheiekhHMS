using ElsheiekhHMS.Application.Appointments.Contracts;
using ElsheiekhHMS.Application.Common.Validation;

namespace ElsheiekhHMS.Application.Appointments.Validation;

public sealed class CancelAppointmentRequestValidator : IRequestValidator<CancelAppointmentRequest>
{
    public ValidationResult Validate(CancelAppointmentRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var errors = new List<ValidationError>();
        if (request.AppointmentId <= 0) errors.Add(new(nameof(request.AppointmentId), "appointment.id.positive", "Appointment ID must be positive."));
        if (request.Reason is not null && request.Reason.Length > 1000) errors.Add(new(nameof(request.Reason), "appointment.reason.maximum", "Cancellation reason cannot exceed 1000 characters."));
        return new ValidationResult(errors);
    }
}
