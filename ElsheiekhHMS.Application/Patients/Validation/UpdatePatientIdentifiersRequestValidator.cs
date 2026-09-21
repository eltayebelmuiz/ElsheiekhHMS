using ElsheiekhHMS.Application.Common.Validation;
using ElsheiekhHMS.Application.Patients.Contracts;

namespace ElsheiekhHMS.Application.Patients.Validation;

public sealed class UpdatePatientIdentifiersRequestValidator : IRequestValidator<UpdatePatientIdentifiersRequest>
{
    public ValidationResult Validate(UpdatePatientIdentifiersRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var errors = new List<ValidationError>();
        Optional(errors, request.NationalId, nameof(request.NationalId), 100);
        Optional(errors, request.PassportNumber, nameof(request.PassportNumber), 100);
        return new ValidationResult(errors);
    }

    private static void Optional(List<ValidationError> errors, string? value, string field, int max) {
        if (value is not null && value.Length > max) errors.Add(new(field, $"patient.{field}.maximum", $"{field} cannot exceed {max} characters."));
    }
}
