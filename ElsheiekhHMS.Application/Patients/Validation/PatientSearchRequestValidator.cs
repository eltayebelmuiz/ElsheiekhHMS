using ElsheiekhHMS.Application.Common.Contracts;
using ElsheiekhHMS.Application.Common.Validation;
using ElsheiekhHMS.Application.Patients.Contracts;

namespace ElsheiekhHMS.Application.Patients.Validation;

public sealed class PatientSearchRequestValidator : IRequestValidator<PatientSearchRequest>
{
    private readonly PageRequestValidator _pageValidator = new();

    public ValidationResult Validate(PatientSearchRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var errors = new List<ValidationError>();
        Optional(errors, request.SearchText, nameof(request.SearchText), 100);
        Optional(errors, request.PatientCode, nameof(request.PatientCode), 13);
        Optional(errors, request.Phone, nameof(request.Phone), 32);
        Optional(errors, request.NationalId, nameof(request.NationalId), 100);
        Optional(errors, request.PassportNumber, nameof(request.PassportNumber), 100);
        if (request.DateOfBirth == DateOnly.MinValue) errors.Add(new(nameof(request.DateOfBirth), "patient.date_of_birth.invalid", "Date of birth is invalid."));
        errors.AddRange(_pageValidator.Validate(request.Page).Errors);
        if (!Enum.IsDefined(request.SortBy)) errors.Add(new(nameof(request.SortBy), "patient.sort.invalid", "Patient sort field is invalid."));
        if (!Enum.IsDefined(request.SortDirection)) errors.Add(new(nameof(request.SortDirection), "patient.sort_direction.invalid", "Sort direction is invalid."));
        return new ValidationResult(errors);
    }

    private static void Optional(List<ValidationError> errors, string? value, string field, int max) {
        if (value is not null && value.Length > max) errors.Add(new(field, $"patient.{field}.maximum", $"{field} cannot exceed {max} characters."));
    }
}
