namespace Deba.Caching.Models;

public record CacheOptions
{
    private readonly TimeSpan _defaultExpiration = TimeSpan.FromDays(7);

    public TimeSpan Expiration { get; private set; }

    public CacheOptions()
    {
        Expiration = _defaultExpiration;
    }
    
    public CacheOptions(TimeSpan expiration)
    {
        Expiration = expiration;
    }
}