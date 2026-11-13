using System.Text;
using System.Text.Json;
using Deba.Caching.Interfaces;
using Deba.Caching.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Xunit;

namespace Deba.Caching.Tests;

public sealed class CacheCompatibilityTests
{
    [Fact]
    public async Task Memory_cache_returns_stored_values_and_default_on_missing_keys()
    {
        using var provider = BuildMemoryProvider();
        var cache = provider.GetRequiredService<IMemoryCacheService>();

        Assert.Null(await cache.GetItemAsync<string>("missing"));
        Assert.Equal(0, await cache.GetItemAsync<int>("missing-number"));
        await cache.SetItemAsync("hotel", "original");
        Assert.Equal("original", await cache.GetItemAsync<string>("hotel"));
        await cache.SetItemAsync("hotel", "replacement");
        Assert.Equal("replacement", await cache.GetItemAsync<string>("hotel"));
        await cache.SetItemAsync("number", 42);
        Assert.Equal(42, await cache.GetItemAsync<int>("number"));
    }

    [Fact]
    public async Task Memory_get_or_set_runs_callback_only_for_miss_in_sequential_calls()
    {
        using var provider = BuildMemoryProvider();
        var cache = provider.GetRequiredService<IMemoryCacheService>();
        var calls = 0;

        Task<string> Fetch() => Task.FromResult($"value-{++calls}");

        Assert.Equal("value-1", await cache.GetOrSetAsync("hotel", Fetch));
        Assert.Equal("value-1", await cache.GetOrSetAsync("hotel", Fetch));
        Assert.Equal("sync", cache.GetOrSet("sync", () => "sync", null));
        Assert.Equal("sync", cache.GetOrSet<string>("sync", () => throw new InvalidOperationException(), null));
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task Null_memory_value_is_treated_as_miss_by_get_or_set()
    {
        using var provider = BuildMemoryProvider();
        var cache = provider.GetRequiredService<IMemoryCacheService>();
        await cache.SetItemAsync<string?>("null", null);

        Assert.Equal("fresh", await cache.GetOrSetAsync("null", () => Task.FromResult("fresh")));
    }

    [Fact]
    public async Task Memory_cache_honors_absolute_expiration_from_registration_and_per_item_override()
    {
        using var provider = BuildMemoryProvider(new CacheOptions(DateTime.UtcNow.AddMinutes(-1)));
        var cache = provider.GetRequiredService<IMemoryCacheService>();
        await cache.SetItemAsync("expired", "old");
        await cache.SetItemAsync("live", "current", new CacheOptions(DateTime.UtcNow.AddMinutes(5)));

        Assert.Null(await cache.GetItemAsync<string>("expired"));
        Assert.Equal("current", await cache.GetItemAsync<string>("live"));
    }

    [Fact]
    public void Default_options_hold_absolute_date_computed_at_construction()
    {
        var earliest = DateTime.UtcNow.AddDays(7);
        var options = new CacheOptions();
        var latest = DateTime.UtcNow.AddDays(7);

        Assert.InRange(options.Expiration, earliest, latest);
        var expiration = options.Expiration;
        Assert.Equal(expiration, options.Expiration);
        Assert.Equal(DateTimeKind.Utc, options.Expiration.Kind);
    }

    [Fact]
    public async Task Memory_service_is_singleton_within_provider_but_isolated_between_providers()
    {
        using var first = BuildMemoryProvider();
        using var second = BuildMemoryProvider();
        using var scope = first.CreateScope();
        var cache = first.GetRequiredService<IMemoryCacheService>();
        Assert.Same(cache, scope.ServiceProvider.GetRequiredService<IMemoryCacheService>());

        await cache.SetItemAsync("hotel", "shared");
        Assert.Equal("shared", await scope.ServiceProvider.GetRequiredService<IMemoryCacheService>().GetItemAsync<string>("hotel"));
        Assert.Null(await second.GetRequiredService<IMemoryCacheService>().GetItemAsync<string>("hotel"));
    }

    [Fact]
    public async Task Concurrent_memory_misses_may_execute_both_callbacks()
    {
        using var provider = BuildMemoryProvider();
        var cache = provider.GetRequiredService<IMemoryCacheService>();
        var release = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0;
        Task<string> Fetch()
        {
            Interlocked.Increment(ref calls);
            return release.Task;
        }

        var first = cache.GetOrSetAsync("hotel", Fetch);
        var second = cache.GetOrSetAsync("hotel", Fetch);
        Assert.Equal(2, calls);
        release.SetResult("hotel");
        Assert.Equal(new[] { "hotel", "hotel" }, await Task.WhenAll(first, second));
    }

    [Fact]
    public async Task Failed_memory_callback_does_not_prevent_subsequent_write()
    {
        using var provider = BuildMemoryProvider();
        var cache = provider.GetRequiredService<IMemoryCacheService>();
        await Assert.ThrowsAsync<InvalidOperationException>(() => cache.GetOrSetAsync<string>("hotel", () => throw new InvalidOperationException("upstream")));

        Assert.Equal("recovered", await cache.GetOrSetAsync("hotel", () => Task.FromResult("recovered")));
    }

    [Fact]
    public async Task Local_storage_reads_legacy_base64_json_envelope()
    {
        var browser = new FakeBrowserStorage();
        browser.Items["legacy"] = "{\"Value\":\"eyJOYW1lIjoiSG90ZWwifQ==\",\"ExpiresAt\":\"2099-12-31T00:00:00Z\"}";
        using var provider = BuildLocalStorageProvider(browser);
        using var scope = provider.CreateScope();
        var cache = scope.ServiceProvider.GetRequiredService<ILocalStorageCacheService>();

        Assert.Equal(new Hotel("Hotel"), await cache.GetItemAsync<Hotel>("legacy"));
        Assert.True(browser.Items.ContainsKey("legacy"));
    }

    [Fact]
    public async Task Local_storage_writes_same_envelope_and_honors_explicit_absolute_date()
    {
        var browser = new FakeBrowserStorage();
        using var provider = BuildLocalStorageProvider(browser);
        using var scope = provider.CreateScope();
        var cache = scope.ServiceProvider.GetRequiredService<ILocalStorageCacheService>();
        var expiration = new DateTime(2099, 12, 31, 0, 0, 0, DateTimeKind.Utc);
        var hotel = new Hotel("Hotel São Paulo");

        await cache.SetItemAsync("hotel", hotel, new CacheOptions(expiration));

        using var json = JsonDocument.Parse(browser.Items["hotel"]);
        Assert.Equal(expiration, json.RootElement.GetProperty("ExpiresAt").GetDateTime());
        var payload = Encoding.UTF8.GetString(Convert.FromBase64String(json.RootElement.GetProperty("Value").GetString()!));
        Assert.Equal(JsonSerializer.Serialize(hotel), payload);
        Assert.Equal(hotel, await cache.GetItemAsync<Hotel>("hotel"));
    }

    [Fact]
    public async Task Local_storage_expiration_removes_only_expired_key_and_missing_value_can_be_fetched()
    {
        var browser = new FakeBrowserStorage();
        using var provider = BuildLocalStorageProvider(browser);
        using var scope = provider.CreateScope();
        var cache = scope.ServiceProvider.GetRequiredService<ILocalStorageCacheService>();
        await cache.SetItemAsync("expired", "old", new CacheOptions(DateTime.UtcNow.AddMinutes(-1)));
        await cache.SetItemAsync("live", "current");

        Assert.Null(await cache.GetItemAsync<string>("expired"));
        Assert.False(browser.Items.ContainsKey("expired"));
        Assert.Equal("current", await cache.GetItemAsync<string>("live"));
        Assert.Equal("fresh", await cache.GetOrSetAsync("missing", () => Task.FromResult("fresh")));
        await cache.RemoveItemAsync("missing");
        Assert.Null(await cache.GetItemAsync<string>("missing"));
    }

    [Fact]
    public async Task Local_storage_value_type_miss_keeps_existing_default_value_semantics()
    {
        using var provider = BuildLocalStorageProvider(new FakeBrowserStorage());
        using var scope = provider.CreateScope();
        var cache = scope.ServiceProvider.GetRequiredService<ILocalStorageCacheService>();
        var called = false;

        var result = await cache.GetOrSetAsync("missing-int", () =>
        {
            called = true;
            return Task.FromResult(42);
        });

        Assert.Equal(0, result);
        Assert.False(called);
    }

    [Fact]
    public void Local_storage_service_is_scoped()
    {
        using var provider = BuildLocalStorageProvider(new FakeBrowserStorage());
        using var first = provider.CreateScope();
        using var second = provider.CreateScope();

        var cache = first.ServiceProvider.GetRequiredService<ILocalStorageCacheService>();
        Assert.Same(cache, first.ServiceProvider.GetRequiredService<ILocalStorageCacheService>());
        Assert.NotSame(cache, second.ServiceProvider.GetRequiredService<ILocalStorageCacheService>());
    }

    private static ServiceProvider BuildMemoryProvider(CacheOptions? options = null) =>
        new ServiceCollection().AddDebaCaching(ECachingType.MemoryCache, options)
            .BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

    private static ServiceProvider BuildLocalStorageProvider(FakeBrowserStorage browser) =>
        new ServiceCollection().AddSingleton<IJSRuntime>(browser)
            .AddDebaCaching(ECachingType.LocalStorage)
            .BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

    public sealed record Hotel(string Name);

    private sealed class FakeBrowserStorage : IJSRuntime
    {
        public Dictionary<string, string> Items { get; } = new();

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
            InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var key = (string)args![0]!;
            switch (identifier)
            {
                case "localStorage.getItem":
                    return ValueTask.FromResult(Items.TryGetValue(key, out var value) ? (TValue)(object)value : default!);
                case "localStorage.setItem":
                    Items[key] = (string)args[1]!;
                    return ValueTask.FromResult(default(TValue)!);
                case "localStorage.removeItem":
                    Items.Remove(key);
                    return ValueTask.FromResult(default(TValue)!);
                default:
                    throw new InvalidOperationException($"Unexpected JavaScript call: {identifier}");
            }
        }
    }
}
