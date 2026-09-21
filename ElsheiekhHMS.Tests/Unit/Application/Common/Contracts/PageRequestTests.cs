using ElsheiekhHMS.Application.Common.Contracts;
using ElsheiekhHMS.Application.Common.Validation;

namespace ElsheiekhHMS.Tests.Unit.Application.Common.Contracts;

public sealed class PageRequestTests
{
    private readonly PageRequestValidator _validator = new();

    [Fact]
    public void Defaults_to_the_approved_bounded_page()
    {
        var result = _validator.Validate(new PageRequest());

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.Equal(25, new PageRequest().PageSize);
    }

    [Theory]
    [InlineData(0, 25)]
    [InlineData(-1, 25)]
    [InlineData(1, 0)]
    [InlineData(1, -1)]
    [InlineData(1, 251)]
    public void Rejects_invalid_page_coordinates_without_clamping(int pageNumber, int pageSize)
    {
        var request = new PageRequest(pageNumber, pageSize);

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Equal(pageNumber, request.PageNumber);
        Assert.Equal(pageSize, request.PageSize);
    }

    [Fact]
    public void Accepts_the_configured_maximum_page_size()
    {
        var result = _validator.Validate(new PageRequest(1, PageRequestValidator.MaximumPageSize));

        Assert.True(result.IsValid);
    }
}
