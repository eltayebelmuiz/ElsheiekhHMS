using ElsheiekhHMS.Application.Common.Validation;
using ElsheiekhHMS.Application.Departments.Contracts;

namespace ElsheiekhHMS.Application.Departments.Validation;

public sealed class CreateDepartmentRequestValidator : IRequestValidator<CreateDepartmentRequest>
{
    public ValidationResult Validate(CreateDepartmentRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var errors = new List<ValidationError>();
        Required(errors, request.Name, nameof(request.Name), 200);
        Optional(errors, request.Description, nameof(request.Description), 2000);
        Optional(errors, request.PhoneExtension, nameof(request.PhoneExtension), 32);
        return new ValidationResult(errors);
    }

    private static void Required(List<ValidationError> errors, string value, string field, int max) {
        if (string.IsNullOrWhiteSpace(value)) errors.Add(new(field, "department.name.required", "Department name is required."));
        else if (value.Length > max) errors.Add(new(field, "department.name.maximum", $"Department name cannot exceed {max} characters."));
    }

    private static void Optional(List<ValidationError> errors, string? value, string field, int max) {
        if (value is not null && value.Length > max) errors.Add(new(field, $"department.{field}.maximum", $"{field} cannot exceed {max} characters."));
    }
}
