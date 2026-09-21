using ElsheiekhHMS.Application.Common.Validation;
using ElsheiekhHMS.Application.Patients.Contracts;

namespace ElsheiekhHMS.Application.Patients.Validation;

public sealed class UpdatePatientContactDetailsRequestValidator : IRequestValidator<UpdatePatientContactDetailsRequest>
{
    public ValidationResult Validate(UpdatePatientContactDetailsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var errors = new List<ValidationError>();
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
}
