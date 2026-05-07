namespace Addresses.Models;

/// <summary>Canonical US / PR / Island Areas address shape for downstream payloads.</summary>
public sealed class StandardizedAddress
{
    public string? Id { get; set; }
    public string? CanonicalFormatted { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string? PostalCodeExtension { get; set; }
    public double? Longitude { get; set; }
    public double? Latitude { get; set; }
    public MatchStatus MatchStatus { get; set; }
    public string? MatchDetail { get; set; }
    public bool IsValidated { get; set; }
    public IReadOnlyList<string> Warnings { get; set; } = Array.Empty<string>();
    public string? SourceBenchmark { get; set; }
    public int RawMatchCount { get; set; }
    public AddressResolutionSource ResolvedFrom { get; set; }
    public string? TigerLineId { get; set; }
    public string? TigerLineSide { get; set; }
}
