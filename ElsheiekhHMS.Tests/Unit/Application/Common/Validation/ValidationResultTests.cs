using ElsheiekhHMS.Application.Common.Validation;

namespace ElsheiekhHMS.Tests.Unit.Application.Common.Validation;

public sealed class ValidationResultTests
{
    [Fact]
    public void Success_contains_no_errors()
    {
        var result = ValidationResult.Success;

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Preserves_multiple_field_and_global_errors()
    {
        var errors = new[]
        {
            new ValidationError("Name", "name.required", "Name is required."),
            new ValidationError(null, "request.invalid", "The request is invalid.")
        };

        var result = new ValidationResult(errors);

        Assert.False(result.IsValid);
        Assert.Equal(errors, result.Errors);
    }

    [Fact]
    public void Copies_the_input_error_collection()
    {
        var errors = new List<ValidationError>
        {
            new("Name", "name.required", "Name is required.")
        };

        var result = new ValidationResult(errors);
        errors.Clear();

        Assert.Single(result.Errors);
    }
}
