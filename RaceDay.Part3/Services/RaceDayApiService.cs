
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
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
                return null;

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
                return null;

            // Verify the API session.
            using var verifyRequest = new HttpRequestMessage(
                HttpMethod.Get,
                "api/auth/me");

            verifyRequest.Headers.TryAddWithoutValidation(
                "Cookie",
                authenticationCookie);

            using var verifyResponse =
                await _client.SendAsync(verifyRequest);

            if (!verifyResponse.IsSuccessStatusCode)
                return null;

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
        // 2. GET EVENTS
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

            using var response =
                await _client.SendAsync(request);

            if (response.StatusCode == HttpStatusCode.Unauthorized ||
                response.StatusCode == HttpStatusCode.Forbidden)
            {
                throw new UnauthorizedAccessException(
                    "You are not authorised to view events.");
            }

            response.EnsureSuccessStatusCode();

            return await response.Content
                .ReadFromJsonAsync<List<EventViewModel>>()
                ?? new List<EventViewModel>();
        }

        // ======================================
        // 3. REGISTER THROUGH PART 2 API
        // ======================================

        public async Task<(bool Success, string Message)>
            RegisterAsync(RegisterViewModel model)
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

        // ======================================
        // 4. GET PARTICIPANT REGISTRATIONS
        // ======================================

        public async Task<List<MyEnrolmentViewModel>>
            GetMyEnrolmentsAsync(string authenticationCookie)
        {
            if (string.IsNullOrWhiteSpace(authenticationCookie))
            {
                throw new UnauthorizedAccessException(
                    "Your API session has expired.");
            }

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                "api/enrolments/my");

            request.Headers.TryAddWithoutValidation(
                "Cookie",
                authenticationCookie);

            using var response =
                await _client.SendAsync(request);

            if (response.StatusCode == HttpStatusCode.Unauthorized ||
                response.StatusCode == HttpStatusCode.Forbidden)
            {
                throw new UnauthorizedAccessException(
                    "Your API session is no longer authorised.");
            }

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                throw new KeyNotFoundException(
                    "Complete your Participant profile first.");
            }

            response.EnsureSuccessStatusCode();

            return await response.Content
                .ReadFromJsonAsync<List<MyEnrolmentViewModel>>()
                ?? new List<MyEnrolmentViewModel>();
        }

        // ======================================
        // 5. GET PARTICIPANT RESULTS
        // ======================================

        public async Task<List<MyResultViewModel>>
            GetMyResultsAsync(string authenticationCookie)
        {
            if (string.IsNullOrWhiteSpace(authenticationCookie))
            {
                throw new UnauthorizedAccessException(
                    "Your API session has expired.");
            }

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                "api/results/my");

            request.Headers.TryAddWithoutValidation(
                "Cookie",
                authenticationCookie);

            using var response =
                await _client.SendAsync(request);

            if (response.StatusCode == HttpStatusCode.Unauthorized ||
                response.StatusCode == HttpStatusCode.Forbidden)
            {
                throw new UnauthorizedAccessException(
                    "Your API session is no longer authorised.");
            }

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                throw new KeyNotFoundException(
                    "Complete your Participant profile first.");
            }

            response.EnsureSuccessStatusCode();

            return await response.Content
                .ReadFromJsonAsync<List<MyResultViewModel>>()
                ?? new List<MyResultViewModel>();
        }

        // ======================================
        // 6. GET PARTICIPANT PROFILE
        // ======================================

        public async Task<ParticipantProfileViewModel>
            GetParticipantProfileAsync(
                string authenticationCookie)
        {
            if (string.IsNullOrWhiteSpace(authenticationCookie))
            {
                throw new UnauthorizedAccessException(
                    "Your API session has expired.");
            }

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                "api/profiles/participant");

            request.Headers.TryAddWithoutValidation(
                "Cookie",
                authenticationCookie);

            using var response =
                await _client.SendAsync(request);

            if (response.StatusCode == HttpStatusCode.Unauthorized ||
                response.StatusCode == HttpStatusCode.Forbidden)
            {
                throw new UnauthorizedAccessException(
                    "You are not authorised to view this profile.");
            }

            response.EnsureSuccessStatusCode();

            return await response.Content
                .ReadFromJsonAsync<ParticipantProfileViewModel>()
                ?? new ParticipantProfileViewModel();
        }

        // ======================================
        // 7. SAVE PARTICIPANT PROFILE
        // ======================================

        public async Task<(bool Success, string Message)>
            SaveParticipantProfileAsync(
                ParticipantProfileViewModel model,
                string authenticationCookie)
        {
            if (string.IsNullOrWhiteSpace(authenticationCookie))
            {
                throw new UnauthorizedAccessException(
                    "Your API session has expired.");
            }

            using var request = new HttpRequestMessage(
                HttpMethod.Put,
                "api/profiles/participant");

            request.Headers.TryAddWithoutValidation(
                "Cookie",
                authenticationCookie);

            // Only send the fields accepted by the API.
            // The API identifies the user from its own cookie.
            request.Content = JsonContent.Create(new
            {
                model.FirstName,
                model.LastName,
                model.DateOfBirth,
                model.Gender,
                model.PhoneNumber
            });

            using var response =
                await _client.SendAsync(request);

            if (response.StatusCode == HttpStatusCode.Unauthorized ||
                response.StatusCode == HttpStatusCode.Forbidden)
            {
                throw new UnauthorizedAccessException(
                    "You are not authorised to update this profile.");
            }

            if (response.IsSuccessStatusCode)
            {
                return (
                    true,
                    "Profile saved successfully."
                );
            }

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                return (
                    false,
                    "Please check all required profile fields."
                );
            }

            response.EnsureSuccessStatusCode();

            return (
                false,
                "Unable to save your profile."
            );
        }
    }
}

