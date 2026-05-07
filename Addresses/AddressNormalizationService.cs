using System.Globalization;
using System.Text;
using Addresses.Census;
using Addresses.Models;
using Microsoft.Extensions.Options;

namespace Addresses;

public sealed class AddressNormalizationService(
    ICensusGeocoderClient censusClient,
    IOptions<CensusGeocoderOptions> options
) : IAddressNormalizationService
{
    private readonly CensusGeocoderOptions _options = options.Value;

    public string? ValidateInput(AddressInput input, out AddressResolutionSource source)
    {
        source = default;
        var hasLine = !string.IsNullOrWhiteSpace(input.OneLine);
        var hasStructured = !string.IsNullOrWhiteSpace(input.Street)
            || !string.IsNullOrWhiteSpace(input.City)
            || !string.IsNullOrWhiteSpace(input.State)
            || !string.IsNullOrWhiteSpace(input.Zip);

        if (hasLine && hasStructured)
            return "Use either OneLine or structured fields (Street, City, State, Zip), not both.";

        if (hasLine)
        {
            source = AddressResolutionSource.OneLine;
            return null;
        }

        if (!hasStructured)
            return "Provide OneLine or structured fields with at least Street and (City and State) or Zip.";

        if (string.IsNullOrWhiteSpace(input.Street))
            return "Structured mode requires Street.";

        var hasCityState = !string.IsNullOrWhiteSpace(input.City) && !string.IsNullOrWhiteSpace(input.State);
        var hasZip = !string.IsNullOrWhiteSpace(input.Zip);
        if (!hasCityState && !hasZip)
            return "Structured mode requires Street and Zip, or Street with City and State.";

        source = AddressResolutionSource.Structured;
        return null;
    }

    public async Task<StandardizedAddress> GeocodeAsync(AddressInput input, CancellationToken cancellationToken = default)
    {
        var validation = ValidateInput(input, out var source);
        if (validation is not null)
        {
            return ErrorResult(validation, source);
        }

        try
        {
            CensusGeocodeResponse response = source == AddressResolutionSource.OneLine
                ? await censusClient.GeocodeOneLineAsync(input.OneLine!.Trim(), cancellationToken).ConfigureAwait(false)
                : await censusClient.GeocodeStructuredAsync(
                    input.Street!.Trim(),
                    input.City?.Trim(),
                    input.State?.Trim(),
                    input.Zip?.Trim(),
                    cancellationToken).ConfigureAwait(false);

            return Map(input, response, source);
        }
        catch (HttpRequestException ex)
        {
            return new StandardizedAddress
            {
                Id = Guid.NewGuid().ToString("D"),
                MatchStatus = MatchStatus.Error,
                MatchDetail = "Census Geocoder HTTP error.",
                Warnings = new[] { ex.Message },
                ResolvedFrom = source,
                SourceBenchmark = _options.Benchmark,
                IsValidated = false,
                RawMatchCount = 0
            };
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            return new StandardizedAddress
            {
                Id = Guid.NewGuid().ToString("D"),
                MatchStatus = MatchStatus.Error,
                MatchDetail = "Census Geocoder request timed out.",
                Warnings = new[] { ex.Message },
                ResolvedFrom = source,
                SourceBenchmark = _options.Benchmark,
                IsValidated = false,
                RawMatchCount = 0
            };
        }
    }

    public async Task<DeduplicateAddressesResponse> DeduplicateAsync(
        DeduplicateAddressesRequest request,
        CancellationToken cancellationToken = default)
    {
        var indexed = new List<(int Index, StandardizedAddress Address)>();
        for (var i = 0; i < request.Addresses.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var addr = await GeocodeAsync(request.Addresses[i], cancellationToken).ConfigureAwait(false);
            addr.Id ??= Guid.NewGuid().ToString("D");
            indexed.Add((i, addr));
        }

        var buckets = new Dictionary<string, List<(int Index, StandardizedAddress Address)>>(StringComparer.Ordinal);

        foreach (var pair in indexed)
        {
            if (pair.Address.MatchStatus == MatchStatus.NoMatch && !request.IncludeUnresolved)
                continue;

            var key = ResolveDedupeKey(pair.Address, pair.Index);
            if (!buckets.TryGetValue(key, out var list))
            {
                list = [];
                buckets[key] = list;
            }

            list.Add(pair);
        }

        var unique = new List<StandardizedAddress>();
        var groups = new List<DeduplicationGroup>();

        foreach (var (key, members) in buckets)
        {
            var ordered = members.OrderBy(m => m.Index).ToList();
            var rep = CloneRepresentative(ordered[0].Address);
            rep.Id = Guid.NewGuid().ToString("D");
            unique.Add(rep);
            groups.Add(new DeduplicationGroup
            {
                Key = key,
                MemberIndexes = ordered.Select(m => m.Index).ToList()
            });
        }

        return new DeduplicateAddressesResponse
        {
            Unique = unique,
            Groups = groups
        };
    }

    private static StandardizedAddress CloneRepresentative(StandardizedAddress s) =>
        new()
        {
            CanonicalFormatted = s.CanonicalFormatted,
            AddressLine1 = s.AddressLine1,
            AddressLine2 = s.AddressLine2,
            City = s.City,
            State = s.State,
            PostalCode = s.PostalCode,
            PostalCodeExtension = s.PostalCodeExtension,
            Longitude = s.Longitude,
            Latitude = s.Latitude,
            MatchStatus = s.MatchStatus,
            MatchDetail = s.MatchDetail,
            IsValidated = s.IsValidated,
            Warnings = s.Warnings,
            SourceBenchmark = s.SourceBenchmark,
            RawMatchCount = s.RawMatchCount,
            ResolvedFrom = s.ResolvedFrom,
            TigerLineId = s.TigerLineId,
            TigerLineSide = s.TigerLineSide
        };

    private static string ResolveDedupeKey(StandardizedAddress address, int index)
    {
        if (address.MatchStatus is MatchStatus.Matched or MatchStatus.Ambiguous)
            return ComputeNormalizationKey(address);

        if (address.MatchStatus == MatchStatus.NoMatch)
            return $"nomatch:{index}";

        return $"error:{index}";
    }

    internal static string ComputeNormalizationKey(StandardizedAddress s)
    {
        var street = NormalizeToken(s.AddressLine1);
        var city = NormalizeToken(s.City);
        var state = NormalizeToken(s.State);
        var zip = NormalizeZip(s.PostalCode);
        return string.Concat(state, "|", zip, "|", city, "|", street);
    }

    private static string NormalizeToken(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "" : value.Trim().ToUpperInvariant();

    private static string NormalizeZip(string? zip)
    {
        if (string.IsNullOrWhiteSpace(zip)) return "";
        var digits = new string(zip.Where(char.IsAsciiDigit).ToArray());
        return digits.Length >= 5 ? digits[..5] : digits;
    }

    private StandardizedAddress Map(
        AddressInput input,
        CensusGeocodeResponse response,
        AddressResolutionSource resolvedFrom)
    {
        var id = Guid.NewGuid().ToString("D");
        var benchmarkName = response.Result?.Input?.Benchmark?.BenchmarkName
            ?? response.Result?.Input?.Benchmark?.Id
            ?? _options.Benchmark;

        var matches = response.Result?.AddressMatches ?? [];
        var count = matches.Count;

        if (count == 0)
        {
            return new StandardizedAddress
            {
                Id = id,
                MatchStatus = MatchStatus.NoMatch,
                MatchDetail = "No address matches returned by Census Geocoder.",
                CanonicalFormatted = FallbackInputDisplay(input),
                ResolvedFrom = resolvedFrom,
                SourceBenchmark = benchmarkName,
                IsValidated = false,
                RawMatchCount = 0,
                Warnings = Array.Empty<string>()
            };
        }

        var first = matches[0];
        var warnings = new List<string>();
        if (count > 1)
            warnings.Add($"Multiple matches ({count}); using first result.");

        AddInputMismatchWarnings(input, first, warnings);

        var components = first.AddressComponents;
        var line1 = BuildStreetLine(components);
        var (postal, ext) = SplitPostal(components?.Zip);

        var status = count == 1 ? MatchStatus.Matched : MatchStatus.Ambiguous;
        var tiger = first.TigerLine;

        return new StandardizedAddress
        {
            Id = id,
            CanonicalFormatted = first.MatchedAddress?.Trim(),
            AddressLine1 = string.IsNullOrWhiteSpace(line1) ? first.MatchedAddress : line1,
            AddressLine2 = null,
            City = components?.City?.Trim(),
            State = components?.State?.Trim(),
            PostalCode = postal,
            PostalCodeExtension = ext,
            Longitude = first.Coordinates?.X,
            Latitude = first.Coordinates?.Y,
            MatchStatus = status,
            MatchDetail = BuildMatchDetail(tiger, components),
            IsValidated = status == MatchStatus.Matched,
            Warnings = warnings,
            SourceBenchmark = benchmarkName,
            RawMatchCount = count,
            ResolvedFrom = resolvedFrom,
            TigerLineId = tiger?.TigerLineId,
            TigerLineSide = tiger?.Side
        };
    }

    private static void AddInputMismatchWarnings(AddressInput input, CensusAddressMatch first, List<string> warnings)
    {
        if (!string.IsNullOrWhiteSpace(input.Zip) && !string.IsNullOrWhiteSpace(first.AddressComponents?.Zip))
        {
            var inZ = NormalizeZip(input.Zip);
            var outZ = NormalizeZip(first.AddressComponents.Zip);
            if (inZ.Length >= 5 && outZ.Length >= 5 && !string.Equals(inZ[..5], outZ[..5], StringComparison.Ordinal))
                warnings.Add("Input ZIP does not match geocoder ZIP for the first match.");
        }
    }

    private static string BuildMatchDetail(CensusTigerLine? tiger, CensusAddressComponents? components)
    {
        var sb = new StringBuilder();
        if (tiger?.TigerLineId is { } tid)
            sb.Append(CultureInfo.InvariantCulture, $"tigerLineId={tid}");
        if (tiger?.Side is { } side)
        {
            if (sb.Length > 0) sb.Append("; ");
            sb.Append(CultureInfo.InvariantCulture, $"side={side}");
        }

        if (components?.ToAddress is { } to && components.FromAddress is { } from
            && !string.Equals(from, to, StringComparison.OrdinalIgnoreCase))
        {
            if (sb.Length > 0) sb.Append("; ");
            sb.Append("addressRange=");
            sb.Append(from);
            sb.Append('-');
            sb.Append(to);
        }

        return sb.Length == 0 ? "Census address range match." : sb.ToString();
    }

    private static string? BuildStreetLine(CensusAddressComponents? c)
    {
        if (c is null) return null;
        var parts = new[]
        {
            c.FromAddress,
            c.PreDirection,
            c.PreType,
            c.PreQualifier,
            c.StreetName,
            c.SuffixType,
            c.SuffixDirection,
            c.SuffixQualifier
        }.Where(p => !string.IsNullOrWhiteSpace(p));

        var line = string.Join(' ', parts).Trim();
        return string.IsNullOrWhiteSpace(line) ? null : line;
    }

    private static (string? Postal, string? Extension) SplitPostal(string? zip)
    {
        if (string.IsNullOrWhiteSpace(zip)) return (null, null);
        var z = zip.Trim();
        var dash = z.IndexOf('-', StringComparison.Ordinal);
        if (dash > 0)
        {
            var main = z[..dash].Trim();
            var ext = z[(dash + 1)..].Trim();
            return (main, string.IsNullOrWhiteSpace(ext) ? null : ext);
        }

        return (z, null);
    }

    private static string FallbackInputDisplay(AddressInput input)
    {
        if (!string.IsNullOrWhiteSpace(input.OneLine))
            return input.OneLine.Trim();

        var sb = new StringBuilder();
        AppendPart(sb, input.Street);
        AppendPart(sb, input.City);
        AppendPart(sb, input.State);
        AppendPart(sb, input.Zip);
        return sb.ToString().Trim();
    }

    private static void AppendPart(StringBuilder sb, string? part)
    {
        if (string.IsNullOrWhiteSpace(part)) return;
        if (sb.Length > 0) sb.Append(", ");
        sb.Append(part.Trim());
    }

    private static StandardizedAddress ErrorResult(string message, AddressResolutionSource source) =>
        new()
        {
            Id = Guid.NewGuid().ToString("D"),
            MatchStatus = MatchStatus.Error,
            MatchDetail = "Invalid address input.",
            Warnings = new[] { message },
            ResolvedFrom = source,
            IsValidated = false,
            RawMatchCount = 0
        };
}
