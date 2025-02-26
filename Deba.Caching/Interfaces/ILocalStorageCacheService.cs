namespace Deba.Caching.Interfaces;

public interface ILocalStorageCacheService : ICacheProtocol
{
    Task RemoveItemAsync(string itemKey);
}