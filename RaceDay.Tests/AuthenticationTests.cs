
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using RaceDay.Controllers;
using Xunit;

namespace RaceDay.Tests
{
    public class AuthenticationTests
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

        [Fact]
        public void Registration_WithValidDetails_PassesValidation()
        {
            var request = new ApiRegisterRequest
            {
                Email = "test@example.com",
                Password = "Test@12345",
                ConfirmPassword = "Test@12345",
                RequestedRole = "Participant"
            };

            Assert.True(IsValid(request));
        }

        [Fact]
        public void Registration_WithInvalidEmail_FailsValidation()
        {
            var request = new ApiRegisterRequest
            {
                Email = "invalid-email",
                Password = "Test@12345",
                ConfirmPassword = "Test@12345",
                RequestedRole = "Participant"
            };

            Assert.False(IsValid(request));
        }

        [Fact]
        public void Registration_WithMismatchedPasswords_FailsValidation()
        {
            var request = new ApiRegisterRequest
            {
                Email = "test@example.com",
                Password = "Test@12345",
                ConfirmPassword = "Different@123",
                RequestedRole = "Participant"
            };

            Assert.False(IsValid(request));
        }

        [Fact]
        public void Registration_WithInvalidRole_FailsValidation()
        {
            var request = new ApiRegisterRequest
            {
                Email = "test@example.com",
                Password = "Test@12345",
                ConfirmPassword = "Test@12345",
                RequestedRole = "Admin"
            };

            Assert.False(IsValid(request));
        }

        [Fact]
        public void Login_WithMissingPassword_FailsValidation()
        {
            var request = new ApiLoginRequest
            {
                Email = "test@example.com",
                Password = ""
            };

            Assert.False(IsValid(request));
        }

        [Fact]
        public void PasswordHasher_WithCorrectPassword_VerifiesSuccessfully()
        {
            var hasher = new PasswordHasher<IdentityUser>();
            var user = new IdentityUser
            {
                UserName = "test@example.com",
                Email = "test@example.com"
            };

            string password = "Test@12345";

            string hash = hasher.HashPassword(user, password);

            var result = hasher.VerifyHashedPassword(
                user,
                hash,
                password);

            Assert.NotEqual(password, hash);

            Assert.True(
                result == PasswordVerificationResult.Success ||
                result == PasswordVerificationResult.SuccessRehashNeeded);
        }

        [Fact]
        public void PasswordHasher_WithIncorrectPassword_RejectsLogin()
        {
            var hasher = new PasswordHasher<IdentityUser>();
            var user = new IdentityUser
            {
                UserName = "test@example.com",
                Email = "test@example.com"
            };

            string hash = hasher.HashPassword(
                user,
                "Correct@12345");

            var result = hasher.VerifyHashedPassword(
                user,
                hash,
                "Wrong@12345");

            Assert.Equal(
                PasswordVerificationResult.Failed,
                result);
        }
    }
}

