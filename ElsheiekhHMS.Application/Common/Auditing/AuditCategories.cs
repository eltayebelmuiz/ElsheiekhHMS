namespace ElsheiekhHMS.Application.Common.Auditing;

public static class AuditCategories
{
    public const string Security = "SECURITY";
    public const string Identity = "IDENTITY";
    public const string Authorization = "AUTHORIZATION";
    public const string Administration = "ADMINISTRATION";
    public const string Business = "BUSINESS";

    public static IReadOnlyList<string> All { get; } =
    [
        Security,
        Identity,
        Authorization,
        Administration,
        Business
    ];
}
