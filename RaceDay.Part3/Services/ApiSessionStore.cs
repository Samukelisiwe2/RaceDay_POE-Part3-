
using System.Security.Cryptography;
using Microsoft.Extensions.Caching.Memory;

namespace RaceDay.Part3.Services
{
    public class ApiSessionStore
    {
        private readonly IMemoryCache _cache;

        private static readonly TimeSpan SessionLifetime =
            TimeSpan.FromHours(2);

        public ApiSessionStore(IMemoryCache cache)
        {
            _cache = cache;
        }

        // Store an API cookie on the server and
        // return a random session identifier.
        public string CreateSession(string apiCookie)
        {
            if (string.IsNullOrWhiteSpace(apiCookie))
            {
                throw new ArgumentException(
                    "API authentication cookie is required.",
                    nameof(apiCookie));
            }

            var sessionId = Convert.ToHexString(
                RandomNumberGenerator.GetBytes(32));

            _cache.Set(
                GetCacheKey(sessionId),
                apiCookie,
                new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow =
                        SessionLifetime
                });

            return sessionId;
        }

        // Retrieve the API cookie for a valid session.
        public string? GetApiCookie(string? sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                return null;
            }

            return _cache.Get<string>(
                GetCacheKey(sessionId));
        }

        // Remove credentials when a user logs out.
        public void RemoveSession(string? sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                return;
            }

            _cache.Remove(GetCacheKey(sessionId));
        }

        private static string GetCacheKey(
            string sessionId)
        {
            return $"RaceDay:ApiSession:{sessionId}";
        }
    }
}
