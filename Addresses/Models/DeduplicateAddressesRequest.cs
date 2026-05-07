namespace Addresses.Models;

public sealed class DeduplicateAddressesRequest
{
    public IList<AddressInput> Addresses { get; set; } = new List<AddressInput>();

    /// <summary>When true, include inputs that did not geocode as separate singleton groups.</summary>
    public bool IncludeUnresolved { get; set; }
}
