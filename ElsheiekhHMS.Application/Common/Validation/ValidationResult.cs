namespace ElsheiekhHMS.Application.Common.Validation;

public sealed class ValidationResult
{
    public ValidationResult(IEnumerable<ValidationError>? errors = null)
    {
        Errors = Array.AsReadOnly((errors ?? []).ToArray());
    }

    public IReadOnlyList<ValidationError> Errors { get; }

    public bool IsValid => Errors.Count == 0;

    public static ValidationResult Success => new();
}
