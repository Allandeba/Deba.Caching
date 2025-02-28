using Deba.Caching.Models;

namespace Deba.Caching.Interfaces;

public interface ICacheProtocol
{
    Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> getItemCallback);
    Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> getItemCallback, CacheOptions cacheOptions);
}