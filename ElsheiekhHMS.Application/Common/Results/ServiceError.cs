namespace ElsheiekhHMS.Application.Common.Results;

public sealed record ServiceError(
    string Code,
    string Message,
    string? Field = null);
