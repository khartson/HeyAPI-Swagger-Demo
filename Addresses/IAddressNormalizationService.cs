using Addresses.Models;

namespace Addresses;

public interface IAddressNormalizationService
{
    /// <summary>Returns null if valid; otherwise a short validation message.</summary>
    string? ValidateInput(AddressInput input, out AddressResolutionSource source);

    Task<StandardizedAddress> GeocodeAsync(AddressInput input, CancellationToken cancellationToken = default);

    Task<DeduplicateAddressesResponse> DeduplicateAsync(
        DeduplicateAddressesRequest request,
        CancellationToken cancellationToken = default);
}
