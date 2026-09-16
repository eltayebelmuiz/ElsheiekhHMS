using ElsheiekhHMS.Core.Exceptions;
using Xunit;

namespace ElsheiekhHMS.Tests.Unit.Domain.Exceptions;

public class DomainExceptionTests
{
    [Theory]
    [InlineData("domain")]
    [InlineData("business")]
    [InlineData("validation")]
    public void Exceptions_preserve_message(string kind)
    {
        const string message = "The domain operation is invalid.";
        DomainException exception = kind switch
        {
            "business" => new BusinessRuleException(message),
            "validation" => new DomainValidationException(message),
            _ => new DomainException(message)
        };
        Assert.Equal(message, exception.Message);
        Assert.Null(exception.InnerException);
    }

    [Theory]
    [InlineData("domain")]
    [InlineData("business")]
    [InlineData("validation")]
    public void Exceptions_preserve_original_cause(string kind)
    {
        const string message = "The domain operation is invalid.";
        var cause = new InvalidOperationException("Original cause");
        DomainException exception = kind switch
        {
            "business" => new BusinessRuleException(message, cause),
            "validation" => new DomainValidationException(message, cause),
            _ => new DomainException(message, cause)
        };
        Assert.Equal(message, exception.Message);
        Assert.Same(cause, exception.InnerException);
    }
}
