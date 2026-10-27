
using System.Net;
using System.Net.Http.Json;
using RaceDay.Part3.Models.Events;

namespace RaceDay.Part3.Services
{
    public class RaceDayApiService
    {
        private readonly HttpClient _client;

        public RaceDayApiService(IHttpClientFactory factory)
        {
            _client = factory.CreateClient("RaceDayApi");
        }

        public async Task<List<EventViewModel>> GetEventsAsync(
            string? authenticationCookie)
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get, "api/events");

            if (!string.IsNullOrEmpty(authenticationCookie))
            {
                request.Headers.TryAddWithoutValidation(
                    "Cookie", authenticationCookie);
            }

            using var response = await _client.SendAsync(request);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                throw new UnauthorizedAccessException(
                    "Please sign in to view events.");
            }

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<
                List<EventViewModel>>()
                ?? new List<EventViewModel>();
        }
    }
}

