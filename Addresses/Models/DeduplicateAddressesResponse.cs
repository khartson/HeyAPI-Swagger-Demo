namespace Addresses.Models;

public sealed class DeduplicateAddressesResponse
{
    public IReadOnlyList<StandardizedAddress> Unique { get; set; } = Array.Empty<StandardizedAddress>();
    public IReadOnlyList<DeduplicationGroup> Groups { get; set; } = Array.Empty<DeduplicationGroup>();
}
