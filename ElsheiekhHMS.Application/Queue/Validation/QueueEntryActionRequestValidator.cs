using ElsheiekhHMS.Application.Common.Validation;
using ElsheiekhHMS.Application.Queue.Contracts;

namespace ElsheiekhHMS.Application.Queue.Validation;

public sealed class QueueEntryActionRequestValidator : IRequestValidator<QueueEntryActionRequest>
{
    public ValidationResult Validate(QueueEntryActionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var errors = new List<ValidationError>();
        if (request.QueueEntryId <= 0)
        {
            errors.Add(new(nameof(request.QueueEntryId), "queue.entry_id.positive", "Queue entry ID must be positive."));
        }

        if (request.Reason is not null && request.Reason.Length > 1000)
        {
            errors.Add(new(nameof(request.Reason), "queue.reason.maximum", "Reason cannot exceed 1000 characters."));
        }

        return new ValidationResult(errors);
    }
}
