namespace Deba.Caching.Interfaces;

public interface ILocalStorageCacheService : ICacheProtocol
{
    Task RemoveItemAsync(string itemKey);
    Task SetItemAsync<T>(string itemKey, T value);
    Task<T?> GetItemAsync<T>(string itemKey);
}