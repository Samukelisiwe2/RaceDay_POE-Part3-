
using System.Net;
using System.Net.Http.Json;
using RaceDay.Part3.Models;
using RaceDay.Part3.Models.Events;

namespace RaceDay.Part3.Services
{
    public class RaceDayApiService
    {
        private readonly HttpClient _client;

        // Connect to the Part 2 REST API
        public RaceDayApiService(IHttpClientFactory factory)
        {
            _client = factory.CreateClient("RaceDayApi");
        }

        // ======================================
        // LOGIN
        // ======================================

        public async Task<LoginApiResponse?> LoginAsync(
            LoginViewModel model)
        {
            using var response = await _client.PostAsJsonAsync(
                "api/auth/login",
                new
                {
                    model.Email,
                    model.Password,
                    model.RememberMe
                });

            // Reject unsuccessful logins
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            // Read the user's ID, email and roles
            return await response.Content
                .ReadFromJsonAsync<LoginApiResponse>();
        }

        // ======================================
        // GET EVENTS
        // ======================================

        public async Task<List<EventViewModel>> GetEventsAsync(
            string? authenticationCookie)
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                "api/events");

            // Temporary API credential parameter.
            // Only use credentials obtained from a
            // trusted, server-side session.
            if (!string.IsNullOrWhiteSpace(authenticationCookie))
            {
                request.Headers.TryAddWithoutValidation(
                    "Cookie",
                    authenticationCookie);
            }

            using var response = await _client.SendAsync(request);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                throw new UnauthorizedAccessException(
                    "Please sign in to view events.");
            }

            response.EnsureSuccessStatusCode();

            return await response.Content
                .ReadFromJsonAsync<List<EventViewModel>>()
                ?? new List<EventViewModel>();
        }
    }
}


