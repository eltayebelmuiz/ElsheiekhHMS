namespace ElsheiekhHMS.Core.Exceptions;

/// <summary>A value is invalid for the domain, independently of transport validation.</summary>
public sealed class DomainValidationException : DomainException
{
    public DomainValidationException(string message) : base(message) { }

    public DomainValidationException(string message, Exception innerException)
        : base(message, innerException) { }
}
