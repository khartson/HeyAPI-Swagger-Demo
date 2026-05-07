namespace Addresses.Census;

public sealed class CensusGeocoderOptions
{
    public const string SectionName = "CensusGeocoder";

    /// <summary>Base URL including trailing slash, e.g. https://geocoding.geo.census.gov/geocoder/</summary>
    public string BaseUrl { get; set; } = "https://geocoding.geo.census.gov/geocoder/";

    /// <summary>Benchmark id or name (e.g. 4 or Public_AR_Current).</summary>
    public string Benchmark { get; set; } = "Public_AR_Current";

    public int TimeoutSeconds { get; set; } = 30;
}
