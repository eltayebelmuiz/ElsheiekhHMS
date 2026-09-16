using ElsheiekhHMS.Core.Common;
using Xunit;

namespace ElsheiekhHMS.Tests.Unit.Domain.Common;

public class BaseEntityTests
{
    [Fact]
    public void Derived_entity_can_assign_integer_identity()
    {
        BaseEntity entity = new TestEntity(42);
        Assert.Equal(42, entity.Id);
    }

    private sealed class TestEntity : BaseEntity
    {
        public TestEntity(int id) => Id = id;
    }
}
