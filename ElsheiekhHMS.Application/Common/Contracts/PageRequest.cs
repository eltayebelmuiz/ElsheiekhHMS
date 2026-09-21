namespace ElsheiekhHMS.Application.Common.Contracts;

/// <summary>
/// Bounded, one-based page coordinates for server-side list queries.
/// Validation is performed by <see cref="Validation.PageRequestValidator"/>.
/// </summary>
public sealed record PageRequest(int PageNumber = 1, int PageSize = 25);
