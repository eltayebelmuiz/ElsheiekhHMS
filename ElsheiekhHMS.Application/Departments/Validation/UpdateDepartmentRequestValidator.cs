using ElsheiekhHMS.Application.Common.Validation;
using ElsheiekhHMS.Application.Departments.Contracts;

namespace ElsheiekhHMS.Application.Departments.Validation;

public sealed class UpdateDepartmentRequestValidator : IRequestValidator<UpdateDepartmentRequest>
{
    public ValidationResult Validate(UpdateDepartmentRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var errors = new List<ValidationError>();
        if (string.IsNullOrWhiteSpace(request.Name)) errors.Add(new(nameof(request.Name), "department.name.required", "Department name is required."));
        else if (request.Name.Length > 200) errors.Add(new(nameof(request.Name), "department.name.maximum", "Department name cannot exceed 200 characters."));
        if (request.Description is not null && request.Description.Length > 2000) errors.Add(new(nameof(request.Description), "department.description.maximum", "Description cannot exceed 2000 characters."));
        if (request.PhoneExtension is not null && request.PhoneExtension.Length > 32) errors.Add(new(nameof(request.PhoneExtension), "department.phone_extension.maximum", "Phone extension cannot exceed 32 characters."));
        return new ValidationResult(errors);
    }
}
