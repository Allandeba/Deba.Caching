namespace Deba.Caching.Models;

public class DebaCachingException : Exception
{
    public DebaCachingException(string? message) : base(message)
    {
    }
}