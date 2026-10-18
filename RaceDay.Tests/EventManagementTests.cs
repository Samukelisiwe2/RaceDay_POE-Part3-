
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using RaceDay.Models;
using Xunit;

namespace RaceDay.Tests
{
    public class EventManagementTests
    {
        private static bool IsValid(EventFormViewModel model)
        {
            var context = new ValidationContext(model);

            var errors = new List<ValidationResult>();

            return Validator.TryValidateObject(
                model,
                context,
                errors,
                validateAllProperties: true);
        }

        private static EventFormViewModel CreateValidEvent()
        {
            return new EventFormViewModel
            {
                EventName = "Durban Test Run",
                Description = "A running event for testing.",
                EventDate = DateTime.Today.AddDays(30),
                ClosingDate = DateTime.Today.AddDays(25),
                Location = "Durban",
                EventType = "Run",
                Status = "Open"
            };
        }

        [Fact]
        public void Event_WithValidDetails_PassesValidation()
        {
            var model = CreateValidEvent();

            Assert.True(IsValid(model));
        }

        [Fact]
        public void Event_WithoutName_FailsValidation()
        {
            var model = CreateValidEvent();

            model.EventName = "";

            Assert.False(IsValid(model));
        }

        [Fact]
        public void Event_WithNameOver150Characters_FailsValidation()
        {
            var model = CreateValidEvent();

            model.EventName = new string('A', 151);

            Assert.False(IsValid(model));
        }

        [Fact]
        public void Event_WithoutDescription_FailsValidation()
        {
            var model = CreateValidEvent();

            model.Description = "";

            Assert.False(IsValid(model));
        }

        [Fact]
        public void Event_WithoutLocation_FailsValidation()
        {
            var model = CreateValidEvent();

            model.Location = "";

            Assert.False(IsValid(model));
        }

        [Theory]
        [InlineData("Run")]
        [InlineData("Walk")]
        [InlineData("Cycle")]
        public void Event_WithSupportedType_PassesValidation(
            string eventType)
        {
            var model = CreateValidEvent();

            model.EventType = eventType;

            Assert.True(IsValid(model));
        }

        [Theory]
        [InlineData("Swimming")]
        [InlineData("Football")]
        [InlineData("Invalid")]
        public void Event_WithUnsupportedType_FailsValidation(
            string eventType)
        {
            var model = CreateValidEvent();

            model.EventType = eventType;

            Assert.False(IsValid(model));
        }

        [Theory]
        [InlineData("Planned")]
        [InlineData("Open")]
        [InlineData("Closed")]
        [InlineData("Completed")]
        [InlineData("Cancelled")]
        public void Event_WithValidStatus_PassesValidation(
            string status)
        {
            var model = CreateValidEvent();

            model.Status = status;

            Assert.True(IsValid(model));
        }

        [Fact]
        public void Event_WithInvalidStatus_FailsValidation()
        {
            var model = CreateValidEvent();

            model.Status = "Unknown";

            Assert.False(IsValid(model));
        }

        [Fact]
        public void NewEvent_DefaultType_ShouldBeRun()
        {
            var model = new EventFormViewModel();

            Assert.Equal("Run", model.EventType);
        }

        [Fact]
        public void NewEvent_DefaultStatus_ShouldBePlanned()
        {
            var model = new EventFormViewModel();

            Assert.Equal("Planned", model.Status);
        }

        [Fact]
        public void NewEvent_ShouldHaveEmptyCategoryCollection()
        {
            var raceEvent = new Event();

            Assert.NotNull(raceEvent.RaceCategories);
            Assert.Empty(raceEvent.RaceCategories);
        }
    }
}
