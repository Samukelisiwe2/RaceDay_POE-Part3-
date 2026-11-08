

using System.Net;
using System.Net.Http.Json;
using RaceDay.Part3.Models;
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

        // ======================================
        // 1. LOGIN THROUGH PART 2 API
        // ======================================

        public async Task<ApiLoginResult?> LoginAsync(
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

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            // Retrieve the API authentication cookie.
            if (!response.Headers.TryGetValues(
                "Set-Cookie", out var cookies))
            {
                return null;
            }

            const string identityCookieName =
                ".AspNetCore.Identity.Application";

            var authenticationCookie = cookies
                .Select(cookie => cookie.Split(';', 2)[0])
                .FirstOrDefault(cookie =>
                    cookie.StartsWith(
                        identityCookieName + "=",
                        StringComparison.Ordinal));

            if (string.IsNullOrWhiteSpace(authenticationCookie))
            {
                return null;
            }

            // Verify the cookie against the Part 2 API.
            using var verifyRequest = new HttpRequestMessage(
                HttpMethod.Get,
                "api/auth/me");

            verifyRequest.Headers.TryAddWithoutValidation(
                "Cookie",
                authenticationCookie);

            using var verifyResponse =
                await _client.SendAsync(verifyRequest);

            if (!verifyResponse.IsSuccessStatusCode)
            {
                return null;
            }

            var verifiedUser = await verifyResponse.Content
                .ReadFromJsonAsync<LoginApiResponse>();

            if (verifiedUser == null ||
                string.IsNullOrWhiteSpace(verifiedUser.Id) ||
                string.IsNullOrWhiteSpace(verifiedUser.Email) ||
                !string.Equals(
                    verifiedUser.Email,
                    model.Email,
                    StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return new ApiLoginResult
            {
                User = verifiedUser,
                AuthenticationCookie = authenticationCookie
            };
        }

        // ======================================
        // 2. GET EVENTS THROUGH PART 2 API
        // ======================================

        public async Task<List<EventViewModel>> GetEventsAsync(
            string? authenticationCookie)
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                "api/events");

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

        // ======================================
        // 3. REGISTER THROUGH PART 2 API
        // ======================================

        public async Task<(bool Success, string Message)> RegisterAsync(
            RegisterViewModel model)
        {
            using var response = await _client.PostAsJsonAsync(
                "api/auth/register",
                new
                {
                    model.Email,
                    model.Password,
                    model.ConfirmPassword,
                    model.RequestedRole
                });

            if (response.StatusCode == HttpStatusCode.Accepted)
            {
                return (
                    true,
                    "Registration submitted successfully. " +
                    "Please confirm your email and wait " +
                    "for Admin approval of your role."
                );
            }

            if (response.StatusCode == HttpStatusCode.Conflict)
            {
                return (
                    false,
                    "An account with this email already exists."
                );
            }

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                return (
                    false,
                    "Registration details were rejected. " +
                    "Check your information and password."
                );
            }

            return (
                false,
                "Registration could not be completed. " +
                "Please try again."
            );
        }
    }
}
