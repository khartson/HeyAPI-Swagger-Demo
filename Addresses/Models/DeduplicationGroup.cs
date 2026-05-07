namespace Addresses.Models;

public sealed class DeduplicationGroup
{
    public string Key { get; set; } = string.Empty;
    public IReadOnlyList<int> MemberIndexes { get; set; } = Array.Empty<int>();
}
