using ElsheiekhHMS.Application.Common.Validation;
using ElsheiekhHMS.Application.Queue.Contracts;

namespace ElsheiekhHMS.Application.Queue.Validation;

public sealed class SendToDoctorRequestValidator : IRequestValidator<SendToDoctorRequest>
{
    public ValidationResult Validate(SendToDoctorRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var errors = new List<ValidationError>();
        if (request.QueueEntryId <= 0) errors.Add(new(nameof(request.QueueEntryId), "queue.entry_id.positive", "Queue entry ID must be positive."));
        if (request.DoctorId <= 0) errors.Add(new(nameof(request.DoctorId), "queue.doctor_id.positive", "Doctor ID must be positive."));
        return new ValidationResult(errors);
    }
}
