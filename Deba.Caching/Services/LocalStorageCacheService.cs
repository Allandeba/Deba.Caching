using System.Text;
using System.Text.Json;
using Blazored.LocalStorage;
using Deba.Caching.Interfaces;
using Deba.Caching.Models;

namespace Deba.Caching.Services;

internal class LocalStorageCacheService : ILocalStorageCacheService
{
    private readonly ILocalStorageService _localStorage;
    private readonly CacheOptions _options;

    public LocalStorageCacheService(ILocalStorageService localStorage, CacheOptions? options)
    {
        _localStorage = localStorage;
        _options = options ?? new();
    }
    
    private bool IsExpired(CacheItem cacheItem)
    {
        if (cacheItem is null) return true;
        return cacheItem.ExpiresAt.CompareTo(DateTime.UtcNow) < 0;
    }
    
    private async Task<CacheItem?> GetFromLocalStorageAsync(string key) =>
        await _localStorage.GetItemAsync<CacheItem>(key);
    
    private async Task SetCacheAsync<T>(string key, T value)
    {
        string jsonString = JsonSerializer.Serialize(value);
        byte[] byteArray = Encoding.UTF8.GetBytes(jsonString);
        string base64String = Convert.ToBase64String(byteArray);

        var item = new CacheItem
        {
            Value = base64String,
            ExpiresAt = _options.Expiration,
        };

        await _localStorage.SetItemAsync(key, item);
    }

    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> getItemCallback)
    {
        var cachedItem = await GetFromLocalStorageAsync(key);
        if (cachedItem is not null && !IsExpired(cachedItem))
            return DecodeObject<T>(cachedItem.Value);

        var freshValue = await getItemCallback();
        await SetCacheAsync(key, freshValue);
        return freshValue;
    }

    private T DecodeObject<T>(string base64)
    {
        var decodedString = Convert.FromBase64String(base64);
        return JsonSerializer.Deserialize<T>(decodedString)!;
    }

    public async Task RemoveItemAsync(string itemKey) =>
        await _localStorage.RemoveItemAsync(itemKey);
}