using ElsheiekhHMS.Application.Common.Contracts;

namespace ElsheiekhHMS.Application.Common.Validation;

public sealed class PageRequestValidator : IRequestValidator<PageRequest>
{
    public const int DefaultPageSize = 25;
    public const int MaximumPageSize = 250;

    public ValidationResult Validate(PageRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var errors = new List<ValidationError>();
        if (request.PageNumber < 1)
        {
            errors.Add(new(
                nameof(PageRequest.PageNumber),
                "pagination.page_number.positive",
                "Page number must be greater than zero."));
        }

        if (request.PageSize < 1)
        {
            errors.Add(new(
                nameof(PageRequest.PageSize),
                "pagination.page_size.positive",
                "Page size must be greater than zero."));
        }
        else if (request.PageSize > MaximumPageSize)
        {
            errors.Add(new(
                nameof(PageRequest.PageSize),
                "pagination.page_size.maximum",
                $"Page size cannot exceed {MaximumPageSize}."));
        }

        return new ValidationResult(errors);
    }
}
