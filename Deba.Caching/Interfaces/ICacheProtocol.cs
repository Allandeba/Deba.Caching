namespace Deba.Caching.Interfaces;

public interface ICacheProtocol
{
    Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> getItemCallback);       
}