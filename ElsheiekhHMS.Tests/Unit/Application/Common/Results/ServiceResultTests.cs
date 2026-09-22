using ElsheiekhHMS.Application.Common.Results;

namespace ElsheiekhHMS.Tests.Unit.Application.Common.Results;

public sealed class ServiceResultTests
{
    [Fact]
    public void Success_preserves_value_and_has_no_errors()
    {
        var result = ServiceResult<int>.Success(42);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Failure_preserves_all_errors_and_has_no_value()
    {
        var errors = new[]
        {
            new ServiceError("patient.first_name.required", "First name is required.", "FirstName"),
            new ServiceError("patient.last_name.required", "Last name is required.", "LastName")
        };

        var result = ServiceResult<int>.Failure(errors);

        Assert.False(result.IsSuccess);
        Assert.Equal(0, result.Value);
        Assert.Equal(errors, result.Errors);
    }
}
