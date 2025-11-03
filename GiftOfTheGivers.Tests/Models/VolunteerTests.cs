using Microsoft.VisualStudio.TestTools.UnitTesting;
using APPR_ST10278170_POE_PART_2.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace GiftOfTheGivers.Tests.Models
{
    [TestClass]
    public class VolunteerTests
    {
        private static IList<ValidationResult> ValidateModel(object model)
        {
            var results = new List<ValidationResult>();
            var ctx = new ValidationContext(model, serviceProvider: null, items: null);
            Validator.TryValidateObject(model, ctx, results, validateAllProperties: true);
            if (model is IValidatableObject validatable)
            {
                results.AddRange(validatable.Validate(ctx));
            }
            return results;
        }

        private static bool HasMemberValidation(IList<ValidationResult> results, string memberName)
        {
            if (results == null) return false;

            if (results.Any(r => r.MemberNames != null && r.MemberNames.Any(m => string.Equals(m, memberName, StringComparison.Ordinal))))
                return true;

            if (results.Any(r => !string.IsNullOrEmpty(r.ErrorMessage) &&
                                 r.ErrorMessage.IndexOf(memberName, StringComparison.OrdinalIgnoreCase) >= 0))
                return true;

            return false;
        }

        [TestMethod]
        public void Volunteer_ValidModel_NoValidationErrors()
        {
            var model = new Volunteer
            {
                FullName = "Sizwe M",
                ContactNumber = "+27123456789",
                Email = "sizwe@example.com",
                Skills = "Medical, Logistics",
                AvailableFrom = DateTime.UtcNow,
                PreferredLocation = "Khayelitsha",
                IsAssigned = false
            };

            var results = ValidateModel(model);
            Assert.AreEqual(0, results.Count, $"Unexpected validation errors: {string.Join("; ", results.Select(r => r.ErrorMessage))}");
        }

        [TestMethod]
        public void Volunteer_MissingRequiredFields_ValidationErrors()
        {
            var model = new Volunteer
            {
                FullName = string.Empty,
                ContactNumber = string.Empty,
                Email = null,
                Skills = string.Empty,
                AvailableFrom = DateTime.UtcNow,
                PreferredLocation = null,
                IsAssigned = false
            };

            var results = ValidateModel(model);

            Assert.IsTrue(HasMemberValidation(results, nameof(Volunteer.FullName)), "Expected validation error for FullName");
            Assert.IsTrue(HasMemberValidation(results, nameof(Volunteer.ContactNumber)), "Expected validation error for ContactNumber");
            Assert.IsTrue(HasMemberValidation(results, nameof(Volunteer.Skills)), "Expected validation error for Skills");
        }

        [TestMethod]
        public void Volunteer_InvalidPhone_ValidationError()
        {
            var model = new Volunteer
            {
                FullName = "A Person",
                ContactNumber = "not-a-phone",
                Email = "valid@example.com",
                Skills = "Logistics",
                AvailableFrom = DateTime.UtcNow
            };

            var results = ValidateModel(model);

            Assert.IsTrue(results.Any(r => r.ErrorMessage != null && r.ErrorMessage.IndexOf("phone", StringComparison.OrdinalIgnoreCase) >= 0)
                || HasMemberValidation(results, nameof(Volunteer.ContactNumber)),
                "Expected validation error for ContactNumber (Phone)");
        }

        [TestMethod]
        public void Volunteer_InvalidEmail_ValidationError()
        {
            var model = new Volunteer
            {
                FullName = "B Person",
                ContactNumber = "+27123456789",
                Email = "not-an-email",
                Skills = "Cooking",
                AvailableFrom = DateTime.UtcNow
            };

            var results = ValidateModel(model);

            Assert.IsTrue(results.Any(r => r.ErrorMessage != null && r.ErrorMessage.IndexOf("email", StringComparison.OrdinalIgnoreCase) >= 0)
                || HasMemberValidation(results, nameof(Volunteer.Email)),
                "Expected validation error for Email (EmailAddress)");
        }

        [TestMethod]
        public void Volunteer_AvailableFrom_DefaultsToNowUtc()
        {
            var before = DateTime.UtcNow.AddSeconds(-1);
            var model = new Volunteer
            {
                FullName = "C Person",
                ContactNumber = "+27123456789",
                Skills = "Admin"
                // AvailableFrom left to default
            };
            var after = DateTime.UtcNow.AddSeconds(1);

            Assert.IsTrue(model.AvailableFrom >= before && model.AvailableFrom <= after,
                $"AvailableFrom {model.AvailableFrom} not within expected window between {before} and {after}");
        }

        [TestMethod]
        public void Volunteer_NullableFields_AreOptional()
        {
            var model = new Volunteer
            {
                FullName = "D Person",
                ContactNumber = "+27123456789",
                Email = null,
                Skills = "Driving",
                AvailableFrom = DateTime.UtcNow,
                PreferredLocation = null
            };

            var results = ValidateModel(model);
            Assert.AreEqual(0, results.Count, $"Expected no validation errors but found: {string.Join("; ", results.Select(r => r.ErrorMessage))}");
        }
    }
}
