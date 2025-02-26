namespace Deba.Caching.Models;

public class CacheItem
{
    public string Value { get; init; } = null!;
    public TimeSpan ExpiresAt { get; init; }
}