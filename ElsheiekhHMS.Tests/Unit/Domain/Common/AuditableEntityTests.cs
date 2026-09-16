using ElsheiekhHMS.Core.Common;
using Xunit;

namespace ElsheiekhHMS.Tests.Unit.Domain.Common;

public class AuditableEntityTests
{
    [Fact]
    public void New_entity_has_no_update_or_user_metadata()
    {
        var entity = new TestEntity();
        Assert.Null(entity.CreatedBy);
        Assert.Null(entity.UpdatedAt);
        Assert.Null(entity.UpdatedBy);
    }

    [Fact]
    public void Creation_metadata_preserves_supplied_utc_time_and_identifier()
    {
        var createdAt = new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);
        var entity = new TestEntity(createdAt, "user-42");
        Assert.Equal(createdAt, entity.CreatedAt);
        Assert.Equal(TimeSpan.Zero, entity.CreatedAt.Offset);
        Assert.Equal("user-42", entity.CreatedBy);
        Assert.Null(entity.UpdatedAt);
    }

    private sealed class TestEntity : AuditableEntity
    {
        public TestEntity() { }

        public TestEntity(DateTimeOffset createdAt, string createdBy)
        {
            CreatedAt = createdAt;
            CreatedBy = createdBy;
        }
    }
}
