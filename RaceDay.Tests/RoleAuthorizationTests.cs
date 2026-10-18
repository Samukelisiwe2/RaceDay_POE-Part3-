

using System;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using RaceDay.Controllers;
using Xunit;

namespace RaceDay.Tests
{
    public class RoleAuthorizationTests
    {
        private static void AssertRequiresRole(
            Type controllerType,
            string actionName,
            string requiredRole)
        {
            var action = controllerType.GetMethods(
                    BindingFlags.Public |
                    BindingFlags.Instance |
                    BindingFlags.DeclaredOnly)
                .FirstOrDefault(m => m.Name == actionName);

            Assert.NotNull(action);

            // The endpoint must not explicitly permit
            // anonymous access.
            Assert.False(
                controllerType.IsDefined(
                    typeof(AllowAnonymousAttribute), true));

            Assert.False(
                action!.IsDefined(
                    typeof(AllowAnonymousAttribute), true));

            // ASP.NET Core combines authorization attributes
            // from the controller AND the action.
            var rules = controllerType
                .GetCustomAttributes<AuthorizeAttribute>(true)
                .Concat(
                    action.GetCustomAttributes<AuthorizeAttribute>(true))
                .ToList();

            Assert.NotEmpty(rules);

            var roleRules = rules
                .Where(r => !string.IsNullOrWhiteSpace(r.Roles))
                .Select(r => r.Roles!
                    .Split(
                        ',',
                        StringSplitOptions.RemoveEmptyEntries |
                        StringSplitOptions.TrimEntries)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase))
                .ToList();

            Assert.NotEmpty(roleRules);

            // The user must satisfy every role-based rule.
            // Multiple roles inside a single rule are OR.
            // Multiple rules are AND.
            var effectiveRoles =
                new System.Collections.Generic.HashSet<string>(
                    roleRules[0],
                    StringComparer.OrdinalIgnoreCase);

            foreach (var roleSet in roleRules.Skip(1))
            {
                effectiveRoles.IntersectWith(roleSet);
            }

            // Confirm only the intended role satisfies
            // these declared role requirements.
            Assert.Single(effectiveRoles);
            Assert.Contains(requiredRole, effectiveRoles);
        }

        [Fact]
        public void EventCreation_RequiresOrganiser()
        {
            AssertRequiresRole(
                typeof(EventManagementApiController),
                "CreateEvent",
                "Organiser");
        }

        [Fact]
        public void EventUpdate_RequiresOrganiser()
        {
            AssertRequiresRole(
                typeof(EventManagementApiController),
                "UpdateEvent",
                "Organiser");
        }

        [Fact]
        public void EventDeletion_RequiresOrganiser()
        {
            AssertRequiresRole(
                typeof(EventManagementApiController),
                "DeleteEvent",
                "Organiser");
        }

        [Fact]
        public void RaceEnrolment_RequiresParticipant()
        {
            AssertRequiresRole(
                typeof(EnrolmentsApiController),
                "CreateEnrolment",
                "Participant");
        }

        [Fact]
        public void ResultCapture_RequiresOrganiser()
        {
            AssertRequiresRole(
                typeof(ResultsApiController),
                "CreateResult",
                "Organiser");
        }

        [Fact]
        public void MyResults_RequiresParticipant()
        {
            AssertRequiresRole(
                typeof(ResultsApiController),
                "GetMyResults",
                "Participant");
        }

        [Fact]
        public void OrganiserProfileUpdate_RequiresOrganiser()
        {
            AssertRequiresRole(
                typeof(ProfilesApiController),
                "UpdateOrganiserProfile",
                "Organiser");
        }

        [Fact]
        public void ParticipantProfileUpdate_RequiresParticipant()
        {
            AssertRequiresRole(
                typeof(ProfilesApiController),
                "UpdateParticipantProfile",
                "Participant");
        }
    }
}
