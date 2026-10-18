
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Data;
using RaceDay.Models;
using Xunit;

namespace RaceDay.Tests
{
    public class AuthenticatedApiTests
    {
        private const string TestPassword = "TestPassword@12345";

        // Create a confirmed test user with an approved role.
        // All accounts exist only inside the test database.
        private static async Task SeedUserAsync(
            RaceDayWebApplicationFactory factory,
            string email,
            string role,
            bool createOrganiserProfile = false)
        {
            using var scope = factory.Services.CreateScope();

            var roles = scope.ServiceProvider
                .GetRequiredService<RoleManager<IdentityRole>>();

            var users = scope.ServiceProvider
                .GetRequiredService<UserManager<IdentityUser>>();

            var db = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            if (!await roles.RoleExistsAsync(role))
            {
                var roleResult =
                    await roles.CreateAsync(new IdentityRole(role));

                Assert.True(roleResult.Succeeded);
            }

            var user = new IdentityUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            var result = await users.CreateAsync(
                user, TestPassword);

            Assert.True(
                result.Succeeded,
                string.Join("; ", result.Errors));

            var addRoleResult =
                await users.AddToRoleAsync(user, role);

            Assert.True(addRoleResult.Succeeded);

            if (createOrganiserProfile)
            {
                db.Organisers.Add(new Organiser
                {
                    OrganiserName = "Test Race Organiser",
                    Email = email,
                    PhoneNumber = "0710000000"
                });

                await db.SaveChangesAsync();
            }
        }

        // Log in through RaceDay's actual API.
        // The returned client keeps the Identity cookie.
        private static async Task LoginAsync(
            HttpClient client,
            string email)
        {
            var response = await client.PostAsJsonAsync(
                "/api/auth/login",
                new
                {
                    email,
                    password = TestPassword,
                    rememberMe = false
                });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        private static object ValidEvent() => new
        {
            eventName = "Integration Test Fun Run",
            description = "Test event created by the Organiser",
            eventDate = DateTime.UtcNow.Date.AddDays(60),
            closingDate = DateTime.UtcNow.Date.AddDays(30),
            location = "Durban",
            eventType = "Run",
            status = "Open"
        };

        // ==========================================
        // TEST 1: ORGANISER CAN CREATE AN EVENT
        // ==========================================

        [Fact]
        public async Task Organiser_CanCreateEvent()
        {
            using var factory = new RaceDayWebApplicationFactory();
            using var client = factory.CreateIsolatedClient();

            await SeedUserAsync(
                factory,
                "organiser1@example.com",
                "Organiser",
                createOrganiserProfile: true);

            await LoginAsync(client, "organiser1@example.com");

            var response = await client.PostAsJsonAsync(
                "/api/events",
                ValidEvent());

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        // ==========================================
        // TEST 2: PARTICIPANT CAN VIEW EVENTS
        // ==========================================

        [Fact]
        public async Task Participant_CanViewEvents()
        {
            using var factory = new RaceDayWebApplicationFactory();
            using var client = factory.CreateIsolatedClient();

            await SeedUserAsync(
                factory,
                "participant1@example.com",
                "Participant");

            await LoginAsync(client, "participant1@example.com");

            var response = await client.GetAsync("/api/events");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        // ==========================================
        // TEST 3: PARTICIPANT CANNOT CREATE EVENTS
        // ==========================================

        [Fact]
        public async Task Participant_CannotCreateEvent()
        {
            using var factory = new RaceDayWebApplicationFactory();
            using var client = factory.CreateIsolatedClient();

            await SeedUserAsync(
                factory,
                "participant2@example.com",
                "Participant");

            await LoginAsync(client, "participant2@example.com");

            var response = await client.PostAsJsonAsync(
                "/api/events",
                ValidEvent());

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        // ==========================================
        // TEST 4: ORGANISER CANNOT EDIT ANOTHER
        // ORGANISER'S EVENT
        // ==========================================

        [Fact]
        public async Task Organiser_CannotEditAnotherOrganisersEvent()
        {
            using var factory = new RaceDayWebApplicationFactory();
            using var client = factory.CreateIsolatedClient();

            await SeedUserAsync(
                factory,
                "owner@example.com",
                "Organiser",
                createOrganiserProfile: true);

            await SeedUserAsync(
                factory,
                "other@example.com",
                "Organiser",
                createOrganiserProfile: true);

            int eventId;

            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();

                var owner = await Microsoft.EntityFrameworkCore
                    .EntityFrameworkQueryableExtensions
                    .FirstAsync(
                        db.Organisers,
                        o => o.Email == "owner@example.com");

                var raceEvent = new RaceDay.Models.Event
                {
                    OrganiserId = owner.OrganiserId,
                    EventName = "Owned Race",
                    Description = "This event belongs to another organiser",
                    EventDate = DateTime.UtcNow.Date.AddDays(60),
                    ClosingDate = DateTime.UtcNow.Date.AddDays(30),
                    Location = "Durban",
                    EventType = "Run",
                    Status = "Open"
                };

                db.Events.Add(raceEvent);
                await db.SaveChangesAsync();

                eventId = raceEvent.EventId;
            }

            await LoginAsync(client, "other@example.com");

            var response = await client.PutAsJsonAsync(
                $"/api/events/{eventId}",
                ValidEvent());

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }
}
