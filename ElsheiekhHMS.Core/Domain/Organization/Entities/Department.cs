using ElsheiekhHMS.Core.Common;
using ElsheiekhHMS.Core.Exceptions;

namespace ElsheiekhHMS.Core.Domain.Organization.Entities;

public sealed class Department : AuditableEntity
{
    public Department(
        string name,
        string? description,
        string? phoneExtension,
        DateTimeOffset createdAt,
        string? createdBy)
    {
        Name = NormalizeName(name);
        Description = description;
        PhoneExtension = phoneExtension;
        IsActive = true;
        CreatedAt = createdAt;
        CreatedBy = createdBy;
    }

    public string Name { get; private set; }

    public string? Description { get; private set; }

    public string? PhoneExtension { get; private set; }

    public bool IsActive { get; private set; }

    public void UpdateDetails(
        string name,
        string? description,
        string? phoneExtension,
        DateTimeOffset updatedAt,
        string? updatedBy)
    {
        EnsureActive();

        var normalizedName = NormalizeName(name);
        Name = normalizedName;
        Description = description;
        PhoneExtension = phoneExtension;
        UpdatedAt = updatedAt;
        UpdatedBy = updatedBy;
    }

    public void Deactivate(DateTimeOffset updatedAt, string? updatedBy)
    {
        EnsureActive();

        IsActive = false;
        UpdatedAt = updatedAt;
        UpdatedBy = updatedBy;
    }

    private void EnsureActive()
    {
        if (!IsActive)
        {
            throw new BusinessRuleException("Inactive departments cannot be changed.");
        }
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainValidationException("Department name is required.");
        }

        return name.Trim();
    }
}
