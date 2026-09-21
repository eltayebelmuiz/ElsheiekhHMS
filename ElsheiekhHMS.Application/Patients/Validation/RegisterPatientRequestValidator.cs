using ElsheiekhHMS.Application.Common.Validation;
using ElsheiekhHMS.Application.Patients.Contracts;
using ElsheiekhHMS.Core.Domain.Patients.Enums;

namespace ElsheiekhHMS.Application.Patients.Validation;

public sealed class RegisterPatientRequestValidator : IRequestValidator<RegisterPatientRequest>
{
    public ValidationResult Validate(RegisterPatientRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var errors = new List<ValidationError>();
        Required(errors, request.FirstName, nameof(request.FirstName), 100);
        Optional(errors, request.MiddleName, nameof(request.MiddleName), 100);
        Optional(errors, request.ThirdName, nameof(request.ThirdName), 100);
        Required(errors, request.LastName, nameof(request.LastName), 100);
        Date(errors, request.DateOfBirth, nameof(request.DateOfBirth));
        EnumValue(errors, request.Gender, nameof(request.Gender));
        if (request.BloodGroup.HasValue) EnumValue(errors, request.BloodGroup.Value, nameof(request.BloodGroup));
        Optional(errors, request.NationalId, nameof(request.NationalId), 100);
        Optional(errors, request.PassportNumber, nameof(request.PassportNumber), 100);
        Required(errors, request.Phone, nameof(request.Phone), 32);
        Required(errors, request.Address, nameof(request.Address), 500);
        Optional(errors, request.City, nameof(request.City), 100);
        Optional(errors, request.EmergencyContactName, nameof(request.EmergencyContactName), 200);
        Optional(errors, request.EmergencyContactPhone, nameof(request.EmergencyContactPhone), 32);
        Optional(errors, request.EmergencyContactRelationship, nameof(request.EmergencyContactRelationship), 100);
        Optional(errors, request.InsuranceProvider, nameof(request.InsuranceProvider), 200);
        return new ValidationResult(errors);
    }

    private static void Required(List<ValidationError> errors, string value, string field, int max) {
        if (string.IsNullOrWhiteSpace(value)) errors.Add(new(field, $"patient.{field}.required", $"{field} is required."));
        else if (value.Length > max) errors.Add(new(field, $"patient.{field}.maximum", $"{field} cannot exceed {max} characters."));
    }

    private static void Optional(List<ValidationError> errors, string? value, string field, int max) {
        if (value is not null && value.Length > max) errors.Add(new(field, $"patient.{field}.maximum", $"{field} cannot exceed {max} characters."));
    }

    private static void Date(List<ValidationError> errors, DateOnly value, string field) {
        if (value == DateOnly.MinValue) errors.Add(new(field, "patient.date_of_birth.required", "Date of birth is required."));
    }

    private static void EnumValue<T>(List<ValidationError> errors, T value, string field) where T : struct, Enum {
        if (!Enum.IsDefined(value)) errors.Add(new(field, $"patient.{field}.invalid", $"{field} is invalid."));
    }
}
