using System.Text.Json.Serialization;

namespace Addresses.Census;

public sealed class CensusGeocodeResponse
{
    [JsonPropertyName("result")]
    public CensusGeocodeResult? Result { get; set; }
}

public sealed class CensusGeocodeResult
{
    [JsonPropertyName("input")]
    public CensusGeocodeInput? Input { get; set; }

    [JsonPropertyName("addressMatches")]
    public List<CensusAddressMatch>? AddressMatches { get; set; }
}

public sealed class CensusGeocodeInput
{
    [JsonPropertyName("benchmark")]
    public CensusBenchmark? Benchmark { get; set; }
}

public sealed class CensusBenchmark
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("benchmarkName")]
    public string? BenchmarkName { get; set; }
}

public sealed class CensusAddressMatch
{
    [JsonPropertyName("matchedAddress")]
    public string? MatchedAddress { get; set; }

    [JsonPropertyName("addressComponents")]
    public CensusAddressComponents? AddressComponents { get; set; }

    [JsonPropertyName("coordinates")]
    public CensusCoordinates? Coordinates { get; set; }

    [JsonPropertyName("tigerLine")]
    public CensusTigerLine? TigerLine { get; set; }
}

public sealed class CensusAddressComponents
{
    [JsonPropertyName("zip")]
    public string? Zip { get; set; }

    [JsonPropertyName("streetName")]
    public string? StreetName { get; set; }

    [JsonPropertyName("preType")]
    public string? PreType { get; set; }

    [JsonPropertyName("city")]
    public string? City { get; set; }

    [JsonPropertyName("preDirection")]
    public string? PreDirection { get; set; }

    [JsonPropertyName("suffixDirection")]
    public string? SuffixDirection { get; set; }

    [JsonPropertyName("fromAddress")]
    public string? FromAddress { get; set; }

    [JsonPropertyName("state")]
    public string? State { get; set; }

    [JsonPropertyName("suffixType")]
    public string? SuffixType { get; set; }

    [JsonPropertyName("toAddress")]
    public string? ToAddress { get; set; }

    [JsonPropertyName("suffixQualifier")]
    public string? SuffixQualifier { get; set; }

    [JsonPropertyName("preQualifier")]
    public string? PreQualifier { get; set; }
}

public sealed class CensusCoordinates
{
    [JsonPropertyName("x")]
    public double X { get; set; }

    [JsonPropertyName("y")]
    public double Y { get; set; }
}

public sealed class CensusTigerLine
{
    [JsonPropertyName("side")]
    public string? Side { get; set; }

    [JsonPropertyName("tigerLineId")]
    public string? TigerLineId { get; set; }
}
