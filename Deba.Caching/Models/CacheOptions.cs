namespace Deba.Caching.Models;

public record CacheOptions
{
    private readonly DateTime _defaultExpiration = DateTime.UtcNow.AddDays(7);

    public DateTime Expiration { get; private set; }

    public CacheOptions()
    {
        Expiration = _defaultExpiration;
    }
    
    public CacheOptions(DateTime expiration)
    {
        Expiration = expiration;
    }
}