using ElsheiekhHMS.Application.Common.Validation;
using ElsheiekhHMS.Application.Departments.Contracts;

namespace ElsheiekhHMS.Application.Departments.Validation;

public sealed class DepartmentSearchRequestValidator : IRequestValidator<DepartmentSearchRequest>
{
    private readonly PageRequestValidator pageValidator = new();

    public ValidationResult Validate(DepartmentSearchRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var errors = new List<ValidationError>();
        if (request.SearchText is not null && request.SearchText.Length > 100)
        {
            errors.Add(new(
                nameof(request.SearchText),
                "department.search_text.maximum",
                "Search text cannot exceed 100 characters."));
        }

        errors.AddRange(pageValidator.Validate(request.Page).Errors);
        if (!Enum.IsDefined(request.SortBy))
        {
            errors.Add(new(nameof(request.SortBy), "department.sort.invalid", "Department sort field is invalid."));
        }

        if (!Enum.IsDefined(request.SortDirection))
        {
            errors.Add(new(nameof(request.SortDirection), "department.sort_direction.invalid", "Sort direction is invalid."));
        }

        return new ValidationResult(errors);
    }
}
