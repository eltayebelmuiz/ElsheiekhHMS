using ElsheiekhHMS.Application.Common.Validation;
using ElsheiekhHMS.Application.Queue.Contracts;

namespace ElsheiekhHMS.Application.Queue.Validation;

public sealed class AddWalkInQueueEntryRequestValidator : IRequestValidator<AddWalkInQueueEntryRequest>
{
    public ValidationResult Validate(AddWalkInQueueEntryRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var errors = new List<ValidationError>();
        Positive(errors, request.PatientId, nameof(request.PatientId));
        Positive(errors, request.DepartmentId, nameof(request.DepartmentId));
        if (!Enum.IsDefined(request.Priority)) errors.Add(new(nameof(request.Priority), "queue.priority.invalid", "Queue priority is invalid."));
        if (request.Notes is not null && request.Notes.Length > 2000) errors.Add(new(nameof(request.Notes), "queue.notes.maximum", "Notes cannot exceed 2000 characters."));
        return new ValidationResult(errors);
    }

    private static void Positive(List<ValidationError> errors, int value, string field) {
        if (value <= 0) errors.Add(new(field, $"queue.{field}.positive", $"{field} must be positive."));
    }
}

public sealed class AddAppointmentQueueEntryRequestValidator : IRequestValidator<AddAppointmentQueueEntryRequest>
{
    public ValidationResult Validate(AddAppointmentQueueEntryRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var errors = new List<ValidationError>();
        Positive(errors, request.PatientId, nameof(request.PatientId));
        Positive(errors, request.DepartmentId, nameof(request.DepartmentId));
        Positive(errors, request.AppointmentId, nameof(request.AppointmentId));
        if (!Enum.IsDefined(request.Priority)) errors.Add(new(nameof(request.Priority), "queue.priority.invalid", "Queue priority is invalid."));
        if (request.Notes is not null && request.Notes.Length > 2000) errors.Add(new(nameof(request.Notes), "queue.notes.maximum", "Notes cannot exceed 2000 characters."));
        return new ValidationResult(errors);
    }

    private static void Positive(List<ValidationError> errors, int value, string field)
    {
        if (value <= 0) errors.Add(new(field, $"queue.{field}.positive", $"{field} must be positive."));
    }
}
