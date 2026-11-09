
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using RaceDay.Part3.Models;
using RaceDay.Part3.Models.Events;
using Microsoft.AspNetCore.Http;

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

            // Verify the login cookie against the Part 2 API.
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

            // Send only the properties accepted by the API.
            // The API identifies the current user from its cookie.
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

        // ======================================
        // 8. GET ORGANISER PROFILE
        // ======================================

        public async Task<OrganiserProfileViewModel>
            GetOrganiserProfileAsync(
                string authenticationCookie)
        {
            if (string.IsNullOrWhiteSpace(authenticationCookie))
            {
                throw new UnauthorizedAccessException(
                    "Your API session has expired.");
            }

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                "api/profiles/organiser");

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
                .ReadFromJsonAsync<OrganiserProfileViewModel>()
                ?? new OrganiserProfileViewModel();
        }

        // ======================================
        // 9. SAVE ORGANISER PROFILE
        // ======================================

        public async Task<(bool Success, string Message)>
            SaveOrganiserProfileAsync(
                OrganiserProfileViewModel model,
                string authenticationCookie)
        {
            if (string.IsNullOrWhiteSpace(authenticationCookie))
            {
                throw new UnauthorizedAccessException(
                    "Your API session has expired.");
            }

            using var request = new HttpRequestMessage(
                HttpMethod.Put,
                "api/profiles/organiser");

            request.Headers.TryAddWithoutValidation(
                "Cookie",
                authenticationCookie);

            request.Content = JsonContent.Create(new
            {
                model.OrganiserName,
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
                    "Organiser profile saved successfully."
                );
            }

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                return (
                    false,
                    "Please check the required profile fields."
                );
            }

            response.EnsureSuccessStatusCode();

            return (
                false,
                "Unable to save organiser profile."
            );
        }
        // ======================================
        // 10. CREATE EVENT THROUGH PART 2 API
        // ======================================

        public async Task<(bool Success, string Message)>
            CreateEventAsync(
                EventCreateViewModel model,
                string authenticationCookie)
        {
            if (string.IsNullOrWhiteSpace(authenticationCookie))
            {
                throw new UnauthorizedAccessException(
                    "Your session has expired.");
            }

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                "api/events");

            request.Headers.TryAddWithoutValidation(
                "Cookie",
                authenticationCookie);

            request.Content = JsonContent.Create(new
            {
                model.EventName,
                model.Description,
                EventDate = model.EventDate,
                ClosingDate = model.ClosingDate,
                model.Location,
                model.EventType,
                model.Status
            });

            using var response = await _client.SendAsync(request);

            if (response.StatusCode == HttpStatusCode.Unauthorized ||
                response.StatusCode == HttpStatusCode.Forbidden)
            {
                throw new UnauthorizedAccessException(
                    "You are not authorised to create events.");
            }

            if (response.StatusCode == HttpStatusCode.Created)
            {
                return (true, "Race event created successfully.");
            }

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                return (
                    false,
                    "The API rejected the event. Check the dates, " +
                    "required fields and your Organiser profile."
                );
            }

            response.EnsureSuccessStatusCode();

            return (false, "Event creation was not completed.");
        }

        // 11. GET ONE EVENT FOR EDITING

        public async Task<EventViewModel?> GetEventAsync(
            int eventId,
            string authenticationCookie)
        {
            if (string.IsNullOrWhiteSpace(authenticationCookie))
                throw new UnauthorizedAccessException();

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"api/events/{eventId}");

            request.Headers.TryAddWithoutValidation(
                "Cookie", authenticationCookie);

            using var response = await _client.SendAsync(request);

            if (response.StatusCode == HttpStatusCode.Unauthorized ||
                response.StatusCode == HttpStatusCode.Forbidden)
                throw new UnauthorizedAccessException();

            if (response.StatusCode == HttpStatusCode.NotFound)
                return null;

            response.EnsureSuccessStatusCode();

            return await response.Content
                .ReadFromJsonAsync<EventViewModel>();
        }


        // 12. UPDATE EXISTING EVENT

        public async Task<(bool Success, string Message)>
            UpdateEventAsync(
                int eventId,
                EventCreateViewModel model,
                string authenticationCookie)
        {
            if (string.IsNullOrWhiteSpace(authenticationCookie))
                throw new UnauthorizedAccessException();

            using var request = new HttpRequestMessage(
                HttpMethod.Put,
                $"api/events/{eventId}");

            request.Headers.TryAddWithoutValidation(
                "Cookie", authenticationCookie);

            request.Content = JsonContent.Create(new
            {
                model.EventName,
                model.Description,
                model.EventDate,
                model.ClosingDate,
                model.Location,
                model.EventType,
                model.Status
            });

            using var response = await _client.SendAsync(request);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
                throw new UnauthorizedAccessException();

            if (response.StatusCode == HttpStatusCode.Forbidden)
                return (false, "You do not own this event.");

            if (response.IsSuccessStatusCode)
                return (true, "Event updated successfully.");

            if (response.StatusCode == HttpStatusCode.NotFound)
                return (false, "Event not found.");

            if (response.StatusCode == HttpStatusCode.BadRequest)
                return (false, "Check the event details and dates.");

            response.EnsureSuccessStatusCode();

            return (false, "Unable to update the event.");
        }
        // ======================================
        // GET EVENT CATEGORIES
        // ======================================

        public async Task<List<RaceCategoryViewModel>>
            GetCategoriesAsync(
                int eventId,
                string authenticationCookie)
        {
            if (string.IsNullOrWhiteSpace(authenticationCookie))
                throw new UnauthorizedAccessException();

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"api/events/{eventId}/categories");

            request.Headers.TryAddWithoutValidation(
                "Cookie",
                authenticationCookie);

            using var response = await _client.SendAsync(request);

            if (response.StatusCode == HttpStatusCode.Unauthorized ||
                response.StatusCode == HttpStatusCode.Forbidden)
                throw new UnauthorizedAccessException();

            if (response.StatusCode == HttpStatusCode.NotFound)
                throw new KeyNotFoundException("Event not found.");

            response.EnsureSuccessStatusCode();

            return await response.Content
                .ReadFromJsonAsync<List<RaceCategoryViewModel>>()
                ?? new List<RaceCategoryViewModel>();
        }


        // ======================================
        // CREATE RACE CATEGORY
        // ======================================

        public async Task<(bool Success, string Message)>
            CreateCategoryAsync(
                CategoryCreateViewModel model,
                string authenticationCookie)
        {
            if (string.IsNullOrWhiteSpace(authenticationCookie))
                throw new UnauthorizedAccessException();

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"api/events/{model.EventId}/categories");

            request.Headers.TryAddWithoutValidation(
                "Cookie",
                authenticationCookie);

            request.Content = JsonContent.Create(new
            {
                model.CategoryName,
                model.DistanceKm,
                model.EntryFee,
                model.MaxParticipants
            });

            using var response = await _client.SendAsync(request);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
                throw new UnauthorizedAccessException();

            if (response.StatusCode == HttpStatusCode.Forbidden)
                return (false, "You do not own this event.");

            if (response.StatusCode == HttpStatusCode.Created)
                return (true, "Race category created successfully.");

            if (response.StatusCode == HttpStatusCode.Conflict)
                return (false, "A category with this name already exists.");

            if (response.StatusCode == HttpStatusCode.BadRequest)
                return (false, "Check the category details and values.");

            if (response.StatusCode == HttpStatusCode.NotFound)
                return (false, "The event was not found.");

            response.EnsureSuccessStatusCode();

            return (false, "Category creation was not completed.");
        }

        // ======================================
        // PARTICIPANT ENTERS A RACE
        // ======================================

        public async Task<(bool Success, string Message)>
            EnterRaceAsync(
                int eventId,
                int categoryId,
                string authenticationCookie)
        {
            if (string.IsNullOrWhiteSpace(authenticationCookie))
            {
                throw new UnauthorizedAccessException(
                    "Your session has expired.");
            }

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"api/events/{eventId}/enrolments");

            request.Headers.TryAddWithoutValidation(
                "Cookie",
                authenticationCookie);

            request.Content = JsonContent.Create(new
            {
                CategoryId = categoryId
            });

            using var response =
                await _client.SendAsync(request);

            if (response.StatusCode ==
                HttpStatusCode.Unauthorized ||
                response.StatusCode ==
                HttpStatusCode.Forbidden)
            {
                throw new UnauthorizedAccessException();
            }

            if (response.StatusCode == HttpStatusCode.Created)
            {
                return (
                    true,
                    "You have successfully entered the race."
                );
            }

            if (response.StatusCode == HttpStatusCode.Conflict)
            {
                return (
                    false,
                    "You may already be registered, " +
                    "or the category is full."
                );
            }

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                return (
                    false,
                    "Registration was rejected. Ensure the " +
                    "event is open and your Participant " +
                    "profile is complete."
                );
            }

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return (false, "The event was not found.");
            }

            response.EnsureSuccessStatusCode();

            return (false, "Registration was not completed.");
        }

        // ======================================
        // GET ENROLLED PARTICIPANTS FOR AN EVENT
        // ======================================

        public async Task<List<EventEnrolmentViewModel>>
            GetEventEnrolmentsAsync(
                int eventId,
                string authenticationCookie)
        {
            if (string.IsNullOrWhiteSpace(authenticationCookie))
            {
                throw new UnauthorizedAccessException(
                    "Your session has expired.");
            }

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"api/events/{eventId}/enrolments");

            request.Headers.TryAddWithoutValidation(
                "Cookie",
                authenticationCookie);

            using var response = await _client.SendAsync(request);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                throw new UnauthorizedAccessException(
                    "Your session has expired.");
            }

            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                throw new System.Security.SecurityException(
                    "You do not own this event.");
            }

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                throw new KeyNotFoundException(
                    "The event was not found.");
            }

            response.EnsureSuccessStatusCode();

            return await response.Content
                .ReadFromJsonAsync<List<EventEnrolmentViewModel>>()
                ?? new List<EventEnrolmentViewModel>();
        }
        // ======================================
        // CREATE RACE RESULT THROUGH PART 2 API
        // ======================================

        public async Task<(bool Success, string Message)>
            CreateResultAsync(
                ResultEntryViewModel model,
                string authenticationCookie)
        {
            if (string.IsNullOrWhiteSpace(authenticationCookie))
            {
                throw new UnauthorizedAccessException(
                    "Your API session has expired.");
            }

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"api/enrolments/{model.EnrollmentId}/results");

            request.Headers.TryAddWithoutValidation(
                "Cookie", authenticationCookie);

            request.Content = JsonContent.Create(new
            {
                model.ResultStatus,
                model.StartTime,
                model.FinishTime,
                model.OverallPosition
            });

            using var response = await _client.SendAsync(request);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
                throw new UnauthorizedAccessException();

            if (response.StatusCode == HttpStatusCode.Forbidden)
                return (false, "You cannot record results for this event.");

            if (response.StatusCode == HttpStatusCode.Created)
                return (true, "Race result recorded successfully.");

            if (response.StatusCode == HttpStatusCode.Conflict)
                return (false,
                    "A result already exists for this participant.");

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                var error = await response.Content
                    .ReadFromJsonAsync<ApiErrorResponse>();

                return (false,
                    error?.Message ??
                    "Check the result details and event date.");
            }

            if (response.StatusCode == HttpStatusCode.NotFound)
                return (false, "The enrolment was not found.");

            response.EnsureSuccessStatusCode();

            return (false, "Result creation was not completed.");
        }

        // Helper for API error messages.
        // Place this class OUTSIDE RaceDayApiService,
        // but inside the RaceDay.Part3.Services namespace.
        public class ApiErrorResponse
        {
            public string? Message { get; set; }
        }

        // ======================================
        // GET RESULTS FOR AN EVENT
        // ======================================

        public async Task<List<EventResultViewModel>>
            GetEventResultsAsync(
                int eventId,
                string authenticationCookie)
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"api/events/{eventId}/results");

            request.Headers.TryAddWithoutValidation(
                "Cookie", authenticationCookie);

            using var response = await _client.SendAsync(request);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
                throw new UnauthorizedAccessException();

            if (response.StatusCode == HttpStatusCode.Forbidden)
                throw new System.Security.SecurityException();

            if (response.StatusCode == HttpStatusCode.NotFound)
                throw new KeyNotFoundException("Event not found.");

            response.EnsureSuccessStatusCode();

            return await response.Content
                .ReadFromJsonAsync<List<EventResultViewModel>>()
                ?? new List<EventResultViewModel>();
        }

        // ======================================
        // UPDATE AN EXISTING RESULT
        // ======================================

        public async Task<(bool Success, string Message)>
            UpdateResultAsync(
                int resultId,
                ResultEntryViewModel model,
                string authenticationCookie)
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Put,
                $"api/results/{resultId}");

            request.Headers.TryAddWithoutValidation(
                "Cookie", authenticationCookie);

            request.Content = JsonContent.Create(new
            {
                model.ResultStatus,
                model.StartTime,
                model.FinishTime,
                model.OverallPosition
            });

            using var response = await _client.SendAsync(request);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
                throw new UnauthorizedAccessException();

            if (response.StatusCode == HttpStatusCode.Forbidden)
                throw new System.Security.SecurityException();

            if (response.StatusCode == HttpStatusCode.NotFound)
                return (false, "Result not found.");

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                var message = await response.Content.ReadAsStringAsync();

                return (false,
                    "Result update was rejected: " + message);
            }

            response.EnsureSuccessStatusCode();

            return (true, "Result updated successfully.");
        }

        // ======================================
        // UPDATE AN EXISTING RACE CATEGORY
        // ======================================

        public async Task<(bool Success, string Message)>
            UpdateCategoryAsync(
                int categoryId,
                CategoryCreateViewModel model,
                string authenticationCookie)
        {
            if (categoryId <= 0 || model.EventId <= 0)
                return (false, "Invalid category or event.");

            if (string.IsNullOrWhiteSpace(authenticationCookie))
                throw new UnauthorizedAccessException();

            using var request = new HttpRequestMessage(
                HttpMethod.Put,
                $"api/categories/{categoryId}");

            request.Headers.TryAddWithoutValidation(
                "Cookie", authenticationCookie);

            request.Content = JsonContent.Create(new
            {
                model.CategoryName,
                model.DistanceKm,
                model.EntryFee,
                model.MaxParticipants
            });

            using var response = await _client.SendAsync(request);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
                throw new UnauthorizedAccessException();

            if (response.StatusCode == HttpStatusCode.Forbidden)
                throw new System.Security.SecurityException(
                    "You cannot edit this category.");

            if (response.StatusCode == HttpStatusCode.NotFound)
                return (false, "Category not found.");

            if (response.IsSuccessStatusCode)
                return (true, "Race category updated successfully.");

            if (response.StatusCode == HttpStatusCode.BadRequest ||
                response.StatusCode == HttpStatusCode.Conflict)
            {
                return (false,
                    "The category update was rejected. Check the " +
                    "values and ensure maximum participants is not " +
                    "less than the number already enrolled.");
            }

            response.EnsureSuccessStatusCode();

            return (false, "Category update was not completed.");
        }

        public async Task<(bool Success, string Message)>
            DeleteCategoryAsync(
                int categoryId,
                string authenticationCookie)
        {
            if (categoryId <= 0)
                return (false, "Invalid category ID.");

            if (string.IsNullOrWhiteSpace(authenticationCookie))
                throw new UnauthorizedAccessException();

            using var request = new HttpRequestMessage(
                HttpMethod.Delete,
                $"api/categories/{categoryId}");

            request.Headers.TryAddWithoutValidation(
                "Cookie", authenticationCookie);

            using var response = await _client.SendAsync(request);

            if (response.StatusCode ==
                System.Net.HttpStatusCode.Unauthorized)
            {
                throw new UnauthorizedAccessException();
            }

            if (response.StatusCode ==
                System.Net.HttpStatusCode.Forbidden)
            {
                throw new System.Security.SecurityException(
                    "You do not have permission to delete this category.");
            }

            if (response.StatusCode ==
                System.Net.HttpStatusCode.NoContent)
            {
                return (true, "Race category deleted successfully.");
            }

            if (response.StatusCode ==
                System.Net.HttpStatusCode.Conflict)
            {
                return (false,
                    "This category cannot be deleted because " +
                    "participants have already enrolled.");
            }

            if (response.StatusCode ==
                System.Net.HttpStatusCode.NotFound)
            {
                return (false, "The category no longer exists.");
            }

            if (response.StatusCode ==
                System.Net.HttpStatusCode.BadRequest)
            {
                return (false, "The API rejected this deletion.");
            }

            response.EnsureSuccessStatusCode();

            return (false, "Category deletion was not confirmed.");
        }

        public async Task<(bool Success, string Message)>
            DeleteEventAsync(
                int eventId,
                string authenticationCookie)
        {
            if (eventId <= 0)
                return (false, "Invalid event ID.");

            if (string.IsNullOrWhiteSpace(authenticationCookie))
                throw new UnauthorizedAccessException();

            using var request = new HttpRequestMessage(
                HttpMethod.Delete,
                $"api/events/{eventId}");

            request.Headers.TryAddWithoutValidation(
                "Cookie",
                authenticationCookie);

            using var response = await _client.SendAsync(request);

            if (response.StatusCode ==
                System.Net.HttpStatusCode.Unauthorized)
            {
                throw new UnauthorizedAccessException();
            }

            if (response.StatusCode ==
                System.Net.HttpStatusCode.Forbidden)
            {
                throw new System.Security.SecurityException(
                    "You do not own this event.");
            }

            if (response.StatusCode ==
                System.Net.HttpStatusCode.NoContent)
            {
                return (true, "Event deleted successfully.");
            }

            if (response.StatusCode ==
                System.Net.HttpStatusCode.Conflict)
            {
                return (false,
                    "This event cannot be deleted because " +
                    "it still has race categories.");
            }

            if (response.StatusCode ==
                System.Net.HttpStatusCode.NotFound)
            {
                return (false, "Event not found.");
            }

            response.EnsureSuccessStatusCode();

            return (false, "Event deletion was not confirmed.");
        }

        public async Task<(byte[] Data, string ContentType)?>
            GetEventBannerAsync(int eventId, string authenticationCookie)
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"api/events/{eventId}/banner");

            request.Headers.TryAddWithoutValidation(
                "Cookie", authenticationCookie);

            using var response = await _client.SendAsync(request);

            if (response.StatusCode == HttpStatusCode.Unauthorized ||
                response.StatusCode == HttpStatusCode.Forbidden)
                throw new UnauthorizedAccessException();

            if (response.StatusCode == HttpStatusCode.NotFound)
                return null;

            response.EnsureSuccessStatusCode();

            string contentType =
                response.Content.Headers.ContentType?.MediaType ?? "";

            if (contentType != "image/jpeg" && contentType != "image/png")
                throw new HttpRequestException("Invalid banner content type.");

            var data = await response.Content.ReadAsByteArrayAsync();

            return (data, contentType);
        }

        public async Task<(bool Success, string Message)>
            UploadEventBannerAsync(
                int eventId,
                IFormFile file,
                string authenticationCookie)
        {
            if (eventId <= 0)
                return (false, "Invalid event ID.");

            if (string.IsNullOrWhiteSpace(authenticationCookie))
                throw new UnauthorizedAccessException();

            if (file == null || file.Length == 0)
                return (false, "Please select an image.");

            if (file.Length > 5 * 1024 * 1024)
                return (false, "Maximum image size is 5 MB.");

            using var form = new MultipartFormDataContent();
            using var stream = file.OpenReadStream();
            using var imageContent = new StreamContent(stream);

            imageContent.Headers.ContentType =
                new System.Net.Http.Headers.MediaTypeHeaderValue(
                    file.ContentType == "image/png"
                        ? "image/png"
                        : "image/jpeg");

            form.Add(imageContent, "file", file.FileName);

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"api/events/{eventId}/banner");

            request.Headers.TryAddWithoutValidation(
                "Cookie",
                authenticationCookie);

            request.Content = form;

            using var response = await _client.SendAsync(request);

            if (response.StatusCode ==
                System.Net.HttpStatusCode.Unauthorized)
                throw new UnauthorizedAccessException();

            if (response.StatusCode ==
                System.Net.HttpStatusCode.Forbidden)
                return (false, "You do not own this event.");

            if (response.StatusCode ==
                System.Net.HttpStatusCode.Created)
                return (true, "Event banner uploaded successfully.");

            if (response.StatusCode ==
                System.Net.HttpStatusCode.BadRequest)
                return (false, "Only valid JPEG or PNG images up to 5 MB are allowed.");

            if (response.StatusCode ==
                System.Net.HttpStatusCode.NotFound)
                return (false, "Event not found.");

            if (!response.IsSuccessStatusCode)
                return (false, "The banner upload failed. Please try again.");

            return (true, "Event banner uploaded successfully.");
        }



    }
}

