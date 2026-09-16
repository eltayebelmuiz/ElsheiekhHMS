using ElsheiekhHMS.Core.Common;
using Xunit;

namespace ElsheiekhHMS.Tests.Unit.Domain.Common;

public class SoftDeletableEntityTests
{
    [Fact]
    public void New_entity_is_active_without_deletion_metadata()
    {
        var entity = new TestEntity();
        Assert.False(entity.IsDeleted);
        Assert.Null(entity.DeletedAt);
        Assert.Null(entity.DeletedBy);
        Assert.Null(entity.UpdatedAt);
    }

    private sealed class TestEntity : SoftDeletableEntity { }
}
