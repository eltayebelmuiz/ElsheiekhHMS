namespace ElsheiekhHMS.Application.Common.Security;

public interface ICurrentUser
{
    bool IsAuthenticated { get; }
    string? UserId { get; }
    string? UserName { get; }
    IReadOnlyCollection<string> Roles { get; }
}
