using System.Security.Claims;
using ElsheiekhHMS.Application.Common.Security;

namespace ElsheiekhHMS.Web.Security;

public sealed class CurrentUserAccessor(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private ClaimsPrincipal _principal =
        httpContextAccessor.HttpContext?.User ?? new ClaimsPrincipal();

    /// <summary>
    /// Updates the scoped principal after Interactive Server authentication-state
    /// validation. The accessor remains scoped and performs no database lookup.
    /// </summary>
    public void SetPrincipal(ClaimsPrincipal principal)
    {
        _principal = principal ?? new ClaimsPrincipal();
    }

    public bool IsAuthenticated => _principal.Identity?.IsAuthenticated == true;

    public string? UserId
    {
        get
        {
            if (!IsAuthenticated)
            {
                return null;
            }

            var userId = _principal.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? _principal.FindFirstValue("sub");
            return string.IsNullOrWhiteSpace(userId) ? null : userId;
        }
    }

    public string? UserName => IsAuthenticated ? _principal.Identity?.Name : null;

    public IReadOnlyCollection<string> Roles => IsAuthenticated
        ? _principal.FindAll(ClaimTypes.Role)
            .Select(claim => claim.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.Ordinal)
            .ToArray()
        : [];
}
