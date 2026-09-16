namespace ElsheiekhHMS.Core.Interfaces;

/// <summary>
/// Opt-in contract for an opaque concurrency token. Infrastructure will configure
/// persistence and conflict detection later; Core does not interpret token bytes.
/// </summary>
public interface IHasConcurrencyToken
{
    byte[] RowVersion { get; set; }
}
