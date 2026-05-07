namespace Addresses.Models;

/// <summary>
/// Address input: use either <see cref="OneLine"/> or structured fields, not both.
/// Structured mode requires street plus (city and state) or zip per Census <c>locations/address</c>.
/// </summary>
public sealed class AddressInput
{
    /// <summary>Full address as a single line (Census <c>onelineaddress</c>).</summary>
    public string? OneLine { get; set; }

    public string? Street { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Zip { get; set; }
}
