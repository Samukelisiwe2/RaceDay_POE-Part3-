
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using RaceDay.Models;
using Xunit;

namespace RaceDay.Tests
{
    public class EnrollmentTests
    {
        private static bool IsValid(object model)
        {
            var context = new ValidationContext(model);
            var errors = new List<ValidationResult>();

            return Validator.TryValidateObject(
                model,
                context,
                errors,
                validateAllProperties: true);
        }

        private static Enrollment CreateValidEnrollment()
        {
            return new Enrollment
            {
                ParticipantId = 1,
                CategoryId = 1,
                EnrollmentDate = DateTime.Today,
                RaceNumber = "RD-TEST123456",
                EnrollmentStatus = "Pending"
            };
        }

        [Fact]
        public void Enrollment_WithValidDetails_PassesValidation()
        {
            var enrollment = CreateValidEnrollment();

            Assert.True(IsValid(enrollment));
        }

        [Fact]
        public void EnrollmentForm_WithValidCategory_PassesValidation()
        {
            var model = new EnrollmentFormViewModel
            {
                EventId = 1,
                EventName = "Durban Test Run",
                CategoryId = 1
            };

            Assert.True(IsValid(model));
        }

        [Fact]
        public void EnrollmentForm_WithoutCategory_FailsValidation()
        {
            var model = new EnrollmentFormViewModel
            {
                EventId = 1,
                EventName = "Durban Test Run",
                CategoryId = 0
            };

            Assert.False(IsValid(model));
        }

        [Fact]
        public void EnrollmentForm_WithNegativeCategory_FailsValidation()
        {
            var model = new EnrollmentFormViewModel
            {
                EventId = 1,
                EventName = "Durban Test Run",
                CategoryId = -1
            };

            Assert.False(IsValid(model));
        }

        [Fact]
        public void Enrollment_WithoutRaceNumber_FailsValidation()
        {
            var enrollment = CreateValidEnrollment();

            enrollment.RaceNumber = "";

            Assert.False(IsValid(enrollment));
        }

        [Fact]
        public void Enrollment_WithLongRaceNumber_FailsValidation()
        {
            var enrollment = CreateValidEnrollment();

            enrollment.RaceNumber = new string('R', 31);

            Assert.False(IsValid(enrollment));
        }

        [Fact]
        public void Enrollment_WithoutStatus_FailsValidation()
        {
            var enrollment = CreateValidEnrollment();

            enrollment.EnrollmentStatus = "";

            Assert.False(IsValid(enrollment));
        }

        [Fact]
        public void Enrollment_WithLongStatus_FailsValidation()
        {
            var enrollment = CreateValidEnrollment();

            enrollment.EnrollmentStatus = new string('S', 31);

            Assert.False(IsValid(enrollment));
        }

        [Fact]
        public void Enrollment_CanBeSetToConfirmed()
        {
            var enrollment = CreateValidEnrollment();

            enrollment.EnrollmentStatus = "Confirmed";

            Assert.Equal("Confirmed", enrollment.EnrollmentStatus);
        }

        [Fact]
        public void Enrollment_CanBeSetToCancelled()
        {
            var enrollment = CreateValidEnrollment();

            enrollment.EnrollmentStatus = "Cancelled";

            Assert.Equal("Cancelled", enrollment.EnrollmentStatus);
        }

        [Fact]
        public void Enrollment_ShouldStartWithEmptyPayments()
        {
            var enrollment = new Enrollment();

            Assert.NotNull(enrollment.Payments);
            Assert.Empty(enrollment.Payments);
        }

        [Fact]
        public void Enrollment_ShouldStartWithoutRaceResult()
        {
            var enrollment = new Enrollment();

            Assert.Null(enrollment.RaceResult);
        }
    }
}
