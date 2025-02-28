using Blazored.LocalStorage;
using Deba.Caching.Interfaces;
using Deba.Caching.Models;
using Deba.Caching.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace Deba.Caching;

public static class DebaCachingExtensions
{
    public static IServiceCollection AddDebaCaching(this IServiceCollection services, ECachingType type, CacheOptions? cacheOptions = null)
    {
        switch (type)
        {
            case ECachingType.LocalStorage:
                services.AddBlazoredLocalStorage();
                services.AddScoped<ILocalStorageCacheService>(serviceProvider =>
                {
                    var storage = serviceProvider.GetRequiredService<ILocalStorageService>();
                    return new LocalStorageCacheService(storage, cacheOptions);
                });
                break;

            case ECachingType.MemoryCache:
                services.AddMemoryCache();
                services.AddSingleton<IMemoryCacheService>(serviceProvider =>
                {
                    var memoryCache = serviceProvider.GetRequiredService<IMemoryCache>();
                    return new MemoryCacheService(memoryCache, cacheOptions);
                });
                break;

            default:
                throw new DebaCachingException($"cacheProtocol {nameof(type)} not implemented");
        }

        return services;
    }
}