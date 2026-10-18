

using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace RaceDay.Tests
{
    public class ApiIntegrationTests
    {
        // ==========================================
        // 1. ANONYMOUS ACCESS TESTS
        // ==========================================

        [Theory]
        [InlineData("/api/auth/me")]
        [InlineData("/api/profiles/participant")]
        [InlineData("/api/profiles/organiser")]
        [InlineData("/api/enrolments/my")]
        [InlineData("/api/results/my")]
        public async Task AnonymousUser_ProtectedEndpoint_IsRejected(
            string endpoint)
        {
            using var factory =
                new RaceDayWebApplicationFactory();

            using var client =
                factory.CreateIsolatedClient();

            var response =
                await client.GetAsync(endpoint);

            Assert.True(
                response.StatusCode == HttpStatusCode.Unauthorized ||
                response.StatusCode == HttpStatusCode.Redirect ||
                response.StatusCode == HttpStatusCode.Found,
                $"Expected 401 or 302 for {endpoint}, " +
                $"but received {(int)response.StatusCode}.");
        }

        // ==========================================
        // 2. EVENTS ACCESS TEST
        // ==========================================

        [Fact]
        public async Task AnonymousUser_EventsEndpoint_IsRejected()
        {
            using var factory =
                new RaceDayWebApplicationFactory();

            using var client =
                factory.CreateIsolatedClient();

            var response =
                await client.GetAsync("/api/events");

            Assert.True(
                response.StatusCode == HttpStatusCode.Unauthorized ||
                response.StatusCode == HttpStatusCode.Redirect ||
                response.StatusCode == HttpStatusCode.Found,
                $"Expected 401 or 302 for /api/events, " +
                $"but received {(int)response.StatusCode}.");
        }

        // ==========================================
        // 3. INVALID LOGIN TEST
        // ==========================================

        [Fact]
        public async Task InvalidLogin_IsRejected()
        {
            using var factory =
                new RaceDayWebApplicationFactory();

            using var client =
                factory.CreateIsolatedClient();

            var request = new
            {
                email = "nonexistent@example.com",
                password = "Wrong@12345",
                rememberMe = false
            };

            var response =
                await client.PostAsJsonAsync(
                    "/api/auth/login",
                    request);

            Assert.Equal(
                HttpStatusCode.Unauthorized,
                response.StatusCode);
        }
    }
}
