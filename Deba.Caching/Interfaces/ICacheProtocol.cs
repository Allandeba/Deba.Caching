using Deba.Caching.Models;

namespace Deba.Caching.Interfaces;

public interface ICacheProtocol
{
    Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> getItemCallback, CacheOptions? cacheOptions = null);
    Task SetItemAsync<T>(string itemKey, T value, CacheOptions? cacheOptions = null);
    Task<T?> GetItemAsync<T>(string itemKey);
}