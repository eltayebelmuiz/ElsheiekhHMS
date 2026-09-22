namespace ElsheiekhHMS.Application.Common.Results;

public sealed class ServiceResult<T>
{
    private ServiceResult(bool isSuccess, T? value, IReadOnlyList<ServiceError> errors)
    {
        IsSuccess = isSuccess;
        Value = value;
        Errors = errors;
    }

    public bool IsSuccess { get; }
    public T? Value { get; }
    public IReadOnlyList<ServiceError> Errors { get; }

    public static ServiceResult<T> Success(T value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new(true, value, Array.Empty<ServiceError>());
    }

    public static ServiceResult<T> Failure(params ServiceError[] errors)
    {
        ArgumentNullException.ThrowIfNull(errors);
        if (errors.Length == 0)
        {
            throw new ArgumentException("At least one service error is required.", nameof(errors));
        }

        return new(false, default, Array.AsReadOnly(errors.ToArray()));
    }
}
