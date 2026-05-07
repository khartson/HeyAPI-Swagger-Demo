namespace Addresses.Census;

public interface ICensusGeocoderClient
{
    Task<CensusGeocodeResponse> GeocodeOneLineAsync(string address, CancellationToken cancellationToken = default);

    Task<CensusGeocodeResponse> GeocodeStructuredAsync(
        string street,
        string? city,
        string? state,
        string? zip,
        CancellationToken cancellationToken = default);
}
