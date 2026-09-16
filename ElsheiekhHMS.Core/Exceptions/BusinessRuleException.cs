namespace ElsheiekhHMS.Core.Exceptions;

/// <summary>A domain operation violates a business rule.</summary>
public sealed class BusinessRuleException : DomainException
{
    public BusinessRuleException(string message) : base(message) { }

    public BusinessRuleException(string message, Exception innerException)
        : base(message, innerException) { }
}
