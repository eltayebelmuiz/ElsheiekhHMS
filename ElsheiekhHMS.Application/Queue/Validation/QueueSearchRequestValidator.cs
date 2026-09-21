using ElsheiekhHMS.Application.Common.Validation;
using ElsheiekhHMS.Application.Queue.Contracts;

namespace ElsheiekhHMS.Application.Queue.Validation;

public sealed class QueueSearchRequestValidator : IRequestValidator<QueueSearchRequest>
{
    private readonly PageRequestValidator _pageValidator = new();

    public ValidationResult Validate(QueueSearchRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var errors = new List<ValidationError>();
        if (request.QueueDate == DateOnly.MinValue) errors.Add(new(nameof(request.QueueDate), "queue.date.required", "Queue date is required."));
        PositiveIfPresent(errors, request.DepartmentId, nameof(request.DepartmentId));
        PositiveIfPresent(errors, request.DoctorId, nameof(request.DoctorId));
        if (request.Status.HasValue) EnumValue(errors, request.Status.Value, nameof(request.Status));
        if (request.Priority.HasValue) EnumValue(errors, request.Priority.Value, nameof(request.Priority));
        errors.AddRange(_pageValidator.Validate(request.Page).Errors);
        EnumValue(errors, request.SortBy, nameof(request.SortBy));
        EnumValue(errors, request.SortDirection, nameof(request.SortDirection));
        return new ValidationResult(errors);
    }

    private static void PositiveIfPresent(List<ValidationError> errors, int? value, string field) {
        if (value.HasValue && value.Value <= 0) errors.Add(new(field, $"queue.{field}.positive", $"{field} must be positive when supplied."));
    }

    private static void EnumValue<T>(List<ValidationError> errors, T value, string field) where T : struct, Enum {
        if (!Enum.IsDefined(value)) errors.Add(new(field, $"queue.{field}.invalid", $"{field} is invalid."));
    }
}
