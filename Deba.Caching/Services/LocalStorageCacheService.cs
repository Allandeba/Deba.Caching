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

    private T DecodeObject<T>(string base64)
    {
        var decodedString = Convert.FromBase64String(base64);
        return JsonSerializer.Deserialize<T>(decodedString)!;
    }
    
    public async Task SetItemAsync<T>(string itemKey, T value, CacheOptions? cacheOptions = null)
    {
        var opt = cacheOptions ?? _options;

        string jsonString = JsonSerializer.Serialize(value);
        byte[] byteArray = Encoding.UTF8.GetBytes(jsonString);
        string base64String = Convert.ToBase64String(byteArray);

        var item = new CacheItem
        {
            Value = base64String,
            ExpiresAt = opt.Expiration,
        };

        await _localStorage.SetItemAsync(itemKey, item);
    }

    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> getItemCallback, CacheOptions? cacheOptions = null)
    {
        var cachedItem = await GetItemAsync<T>(key);
        if (cachedItem is not null)
            return cachedItem;

        var freshValue = await getItemCallback();
        await SetItemAsync(key, freshValue, cacheOptions);
        return freshValue;
    }

    public async Task<T?> GetItemAsync<T>(string itemKey)
    {
        var cachedItem = await GetFromLocalStorageAsync(itemKey);
        if (cachedItem is not null)
        {
            if (!IsExpired(cachedItem))
                if (cachedItem.Value is not null)
                    return DecodeObject<T>(cachedItem.Value);

            await RemoveItemAsync(itemKey);
        }

        return default;
    }

    public async Task RemoveItemAsync(string itemKey) =>
        await _localStorage.RemoveItemAsync(itemKey);
}