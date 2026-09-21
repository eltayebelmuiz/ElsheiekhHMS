using ElsheiekhHMS.Application.Common.Validation;
using ElsheiekhHMS.Application.Patients.Contracts;
using ElsheiekhHMS.Core.Domain.Patients.Enums;

namespace ElsheiekhHMS.Application.Patients.Validation;

public sealed class UpdatePatientDemographicsRequestValidator : IRequestValidator<UpdatePatientDemographicsRequest>
{
    public ValidationResult Validate(UpdatePatientDemographicsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var errors = new List<ValidationError>();
        Required(errors, request.FirstName, nameof(request.FirstName), 100);
        Optional(errors, request.MiddleName, nameof(request.MiddleName), 100);
        Optional(errors, request.ThirdName, nameof(request.ThirdName), 100);
        Required(errors, request.LastName, nameof(request.LastName), 100);
        if (request.DateOfBirth == DateOnly.MinValue) errors.Add(new(nameof(request.DateOfBirth), "patient.date_of_birth.required", "Date of birth is required."));
        EnumValue(errors, request.Gender, nameof(request.Gender));
        if (request.BloodGroup.HasValue) EnumValue(errors, request.BloodGroup.Value, nameof(request.BloodGroup));
        return new ValidationResult(errors);
    }

    private static void Required(List<ValidationError> errors, string value, string field, int max) {
        if (string.IsNullOrWhiteSpace(value)) errors.Add(new(field, $"patient.{field}.required", $"{field} is required."));
        else if (value.Length > max) errors.Add(new(field, $"patient.{field}.maximum", $"{field} cannot exceed {max} characters."));
    }

    private static void Optional(List<ValidationError> errors, string? value, string field, int max) {
        if (value is not null && value.Length > max) errors.Add(new(field, $"patient.{field}.maximum", $"{field} cannot exceed {max} characters."));
    }

    private static void EnumValue<T>(List<ValidationError> errors, T value, string field) where T : struct, Enum {
        if (!Enum.IsDefined(value)) errors.Add(new(field, $"patient.{field}.invalid", $"{field} is invalid."));
    }
}
