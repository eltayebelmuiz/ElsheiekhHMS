using ElsheiekhHMS.Infrastructure;
using ElsheiekhHMS.Infrastructure.Persistence.Allocation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ElsheiekhHMS.Tests.Unit.Infrastructure.Allocation;

public sealed class AllocatorRegistrationTests
{
    [Fact]
    public void Infrastructure_registers_only_the_approved_allocator_implementations()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:ElsheiekhHmsDatabase"] = "Server=unused;Database=ShapeOnly;"
            })
            .Build();

        services.AddInfrastructure(configuration);

        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(PatientCodeAllocator) &&
            descriptor.ImplementationType == typeof(PatientCodeAllocator));
        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(QueueTicketAllocator) &&
            descriptor.ImplementationType == typeof(QueueTicketAllocator));
    }
}
