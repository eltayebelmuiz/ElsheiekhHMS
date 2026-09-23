namespace ElsheiekhHMS.Application.Clinical.ProviderOwnership.Persistence;

public sealed class ProviderOwnershipConflictException : Exception
{
    public ProviderOwnershipConflictException(Exception innerException)
        : base("Provider ownership conflicts with an existing mapping.", innerException)
    {
    }
}
