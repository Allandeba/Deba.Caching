using Deba.Caching.Interfaces;
using Deba.Caching.Models;
using Microsoft.Extensions.Caching.Memory;

namespace Deba.Caching.Services;

internal class MemoryCacheService : IMemoryCacheService
{
    private static readonly SemaphoreSlim Semaphore = new SemaphoreSlim(1, 1);
    private readonly CacheOptions _options;
    private readonly IMemoryCache _memoryCache;

    public MemoryCacheService(IMemoryCache memoryCache, CacheOptions? options)
    {
        _memoryCache = memoryCache;
        _options = options ?? new();
    }

    private async Task<(bool Found, T? Value)> GetCachedItemAsync<T>(string itemKey)
    {
        var hasItem = _memoryCache.TryGetValue(itemKey, out T? cachedItem);
        return await Task.FromResult((hasItem, cachedItem));
    }

    public T GetOrSet<T>(string key, Func<T> getItemCallback, CacheOptions? cacheOptions = null)
    {
        var (found, cachedItem) = Task.Run(() => GetCachedItemAsync<T>(key)).Result;
        if (found && cachedItem is not null) return cachedItem;

        cachedItem = getItemCallback();

        Task.Run(() => SetItemAsync(key, cachedItem, cacheOptions)).Wait();
        return cachedItem;
    }

    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> getItemCallbackAsync, CacheOptions? cacheOptions = null)
    {
        var (found, cachedItem) = await GetCachedItemAsync<T>(key);
        if (found && cachedItem is not null) return cachedItem;

        cachedItem = await getItemCallbackAsync();

        await SetItemAsync(key, cachedItem, cacheOptions);
        return cachedItem;
    }

    public async Task SetItemAsync<T>(string key, T item, CacheOptions? cacheOptions = null)
    {
        var opt = cacheOptions ?? _options;

        var cacheEntryOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(opt.Expiration)
            .SetPriority(CacheItemPriority.Normal)
            .SetSize(1);

        Semaphore.Wait();
        try
        {
            await Task.FromResult(_memoryCache.Set(key, item, cacheEntryOptions));
        }
        finally
        {
            Semaphore.Release();
        }
    }

    public async Task<T?> GetItemAsync<T>(string itemKey)
    {
        var (found, cachedItem) = await GetCachedItemAsync<T>(itemKey);
        return cachedItem;
    }
}