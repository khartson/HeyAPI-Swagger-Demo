namespace Addresses.Models;

/// <summary>Outcome of geocoding against the Census locator.</summary>
public enum MatchStatus
{
    Matched = 0,
    NoMatch = 1,
    Ambiguous = 2,
    Error = 3
}
