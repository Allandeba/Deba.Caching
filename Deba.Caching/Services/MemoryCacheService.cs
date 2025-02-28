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

    public T GetOrSet<T>(string key, Func<T> getItemCallback)
    {
        if (_memoryCache.TryGetValue(key, out T? cachedItem)) return cachedItem!;
        
        cachedItem = getItemCallback();

        SetCache(key, cachedItem);
        return cachedItem;
    }

    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> getItemCallbackAsync)
    {
        if (_memoryCache.TryGetValue(key, out T? cachedItem)) return cachedItem!;

        cachedItem = await getItemCallbackAsync();

        await SetCacheAsync(key, cachedItem);
        return cachedItem;
    }

    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> getItemCallbackAsync, CacheOptions cacheOptions)
    {
        if (_memoryCache.TryGetValue(key, out T? cachedItem)) return cachedItem!;

        cachedItem = await getItemCallbackAsync();

        await SetCacheAsync(key, cachedItem, cacheOptions);
        return cachedItem;
    }

    private async Task SetCacheAsync<T>(string key, T item)
    {
        var cacheEntryOptions = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(_options.Expiration.TimeOfDay)
            .SetAbsoluteExpiration(_options.Expiration.TimeOfDay)
            .SetPriority(CacheItemPriority.Normal)
            .SetSize(1);

        await Semaphore.WaitAsync();
        try
        {
            _memoryCache.Set(key, item, cacheEntryOptions);
        }
        finally
        {
            Semaphore.Release();
        }
    }

    private async Task SetCacheAsync<T>(string key, T item, CacheOptions cacheOptions)
    {
        var cacheEntryOptions = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(cacheOptions.Expiration.TimeOfDay)
            .SetAbsoluteExpiration(cacheOptions.Expiration.TimeOfDay)
            .SetPriority(CacheItemPriority.Normal)
            .SetSize(1);

        await Semaphore.WaitAsync();
        try
        {
            _memoryCache.Set(key, item, cacheEntryOptions);
        }
        finally
        {
            Semaphore.Release();
        }
    }

    private void SetCache<T>(string key, T item)
    {
        var cacheEntryOptions = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(_options.Expiration.TimeOfDay)
            .SetAbsoluteExpiration(_options.Expiration.TimeOfDay)
            .SetPriority(CacheItemPriority.Normal)
            .SetSize(1);

        Semaphore.Wait();
        try
        {
            _memoryCache.Set(key, item, cacheEntryOptions);
        }
        finally
        {
            Semaphore.Release();
        }
    }
}