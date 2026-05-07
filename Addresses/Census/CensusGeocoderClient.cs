using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace Addresses.Census;

public sealed class CensusGeocoderClient(
    HttpClient httpClient,
    IOptions<CensusGeocoderOptions> options
) : ICensusGeocoderClient
{
    private readonly CensusGeocoderOptions _options = options.Value;

    public Task<CensusGeocodeResponse> GeocodeOneLineAsync(string address, CancellationToken cancellationToken = default)
    {
        var query = new Dictionary<string, string?>
        {
            ["address"] = address,
            ["benchmark"] = _options.Benchmark,
            ["format"] = "json"
        };
        var path = QueryHelpers.AddQueryString("locations/onelineaddress", query);
        return GetAsync(path, cancellationToken);
    }

    public Task<CensusGeocodeResponse> GeocodeStructuredAsync(
        string street,
        string? city,
        string? state,
        string? zip,
        CancellationToken cancellationToken = default)
    {
        var query = new Dictionary<string, string?>
        {
            ["street"] = street,
            ["benchmark"] = _options.Benchmark,
            ["format"] = "json"
        };
        if (!string.IsNullOrWhiteSpace(city)) query["city"] = city;
        if (!string.IsNullOrWhiteSpace(state)) query["state"] = state;
        if (!string.IsNullOrWhiteSpace(zip)) query["zip"] = zip;

        var path = QueryHelpers.AddQueryString("locations/address", query);
        return GetAsync(path, cancellationToken);
    }

    private async Task<CensusGeocodeResponse> GetAsync(string relativePath, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, relativePath);
        using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            return new CensusGeocodeResponse();
        }

        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<CensusGeocodeResponse>(cancellationToken).ConfigureAwait(false);
        return body ?? new CensusGeocodeResponse();
    }
}
