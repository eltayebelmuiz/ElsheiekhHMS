namespace ElsheiekhHMS.Application.Common.Validation;

public interface IRequestValidator<in TRequest>
{
    ValidationResult Validate(TRequest request);
}
