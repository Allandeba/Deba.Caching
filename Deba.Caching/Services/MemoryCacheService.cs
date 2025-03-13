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

    public T GetOrSet<T>(string key, Func<T> getItemCallback, CacheOptions? cacheOptions)
    {
        var cachedItem = Task.Run(() => GetItemAsync<T>(key)).Result;
        if (cachedItem is not null) return cachedItem;
        
        cachedItem = getItemCallback();

        Task.Run(() => SetItemAsync(key, cachedItem, cacheOptions)).Wait();
        return cachedItem;
    }

    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> getItemCallbackAsync, CacheOptions? cacheOptions)
    {
        var cachedItem = await GetItemAsync<T>(key);
        if (cachedItem is not null) return cachedItem;

        cachedItem = await getItemCallbackAsync();

        await SetItemAsync(key, cachedItem, cacheOptions);
        return cachedItem;
    }

    public async Task SetItemAsync<T>(string key, T item, CacheOptions? cacheOptions)
    {
        var opt = cacheOptions ?? _options;

        var cacheEntryOptions = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(opt.Expiration.TimeOfDay)
            .SetAbsoluteExpiration(opt.Expiration.TimeOfDay)
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
        _memoryCache.TryGetValue(itemKey, out T? cachedItem);
        return await Task.FromResult(cachedItem);
    }
}