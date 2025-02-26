namespace Deba.Caching.Interfaces;

public interface IMemoryCacheService : ICacheProtocol
{
    T GetOrSet<T>(string key, Func<T> getItemCallback);
}