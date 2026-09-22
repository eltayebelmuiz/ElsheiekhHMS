using System.Reflection;
using ElsheiekhHMS.Application.Common.Contracts;
using ElsheiekhHMS.Application.Common.Results;
using ElsheiekhHMS.Application.Common.Validation;

namespace ElsheiekhHMS.Tests.Unit.Application.Architecture;

public sealed class ApplicationContractArchitectureTests
{
    private static readonly Assembly ApplicationAssembly = typeof(PageRequest).Assembly;

    [Fact]
    public void Application_assembly_does_not_reference_web_or_ef_core()
    {
        var references = ApplicationAssembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.DoesNotContain("ElsheiekhHMS.Web", references);
        Assert.DoesNotContain("Microsoft.EntityFrameworkCore", references);
    }

    [Fact]
    public void Shared_contracts_do_not_expose_web_ef_or_security_fields()
    {
        var contractTypes = new[]
        {
            typeof(PageRequest),
            typeof(PagedResult<>),
            typeof(SortDirection),
            typeof(ServiceError),
            typeof(ServiceResult<>),
            typeof(ValidationError),
            typeof(ValidationResult)
        };
        var forbiddenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "DeletedAt", "DeletedBy",
            "ActorUserId", "Role", "AccountSecurityState", "SecurityStamp", "PasswordHash",
            "ConcurrencyStamp"
        };

        foreach (var type in contractTypes)
        {
            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                Assert.DoesNotContain("Microsoft.AspNetCore", property.PropertyType.FullName);
                Assert.DoesNotContain("Microsoft.EntityFrameworkCore", property.PropertyType.FullName);
                Assert.DoesNotContain(property.Name, forbiddenNames);
            }
        }
    }
}
