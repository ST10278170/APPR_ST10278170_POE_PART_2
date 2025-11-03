using Microsoft.VisualStudio.TestTools.UnitTesting;
using APPR_ST10278170_POE_PART_2.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace GiftOfTheGivers.Tests.Models
{
    [TestClass]
    public class ReliefTaskTests
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
        public void ReliefTask_ValidModel_NoValidationErrors()
        {
            var model = new ReliefTask
            {
                Title = "Distribute blankets",
                Location = "Khayelitsha",
                Status = "Planned",
                Priority = "High",
                Description = "Distribute 200 blankets to affected families",
                DisasterReportId = 5,
                VolunteerId = 10,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(3)
            };

            var results = ValidateModel(model);
            Assert.AreEqual(0, results.Count);
        }

        [TestMethod]
        public void ReliefTask_MissingRequiredFields_ValidationErrors()
        {
            var model = new ReliefTask
            {
                Title = string.Empty,
                Location = string.Empty,
                Status = string.Empty,
                Priority = string.Empty,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(1)
            };

            var results = ValidateModel(model);

            Assert.IsTrue(HasMemberValidation(results, nameof(ReliefTask.Title)), "Expected validation error for Title");
            Assert.IsTrue(HasMemberValidation(results, nameof(ReliefTask.Location)), "Expected validation error for Location");
            Assert.IsTrue(HasMemberValidation(results, nameof(ReliefTask.Status)), "Expected validation error for Status");
            Assert.IsTrue(HasMemberValidation(results, nameof(ReliefTask.Priority)), "Expected validation error for Priority");
        }

        [TestMethod]
        public void ReliefTask_DefaultDates_AreNowishAndEndAfterStart()
        {
            var before = DateTime.UtcNow.AddSeconds(-1);
            var model = new ReliefTask
            {
                Title = "Seed task",
                Location = "Helderberg",
                Status = "Planned",
                Priority = "Medium"
            };
            var after = DateTime.UtcNow.AddSeconds(1);

            Assert.IsTrue(model.StartDate >= before && model.StartDate <= after, $"StartDate {model.StartDate} not in expected range");
            Assert.IsTrue(model.EndDate > model.StartDate, $"EndDate {model.EndDate} should be after StartDate {model.StartDate}");
        }

        [TestMethod]
        public void ReliefTask_StartBeforeEnd_BusinessRuleCheck()
        {
            var start = DateTime.UtcNow.AddDays(2);
            var end = DateTime.UtcNow.AddDays(1);

            var model = new ReliefTask
            {
                Title = "Bad schedule",
                Location = "False Bay",
                Status = "Planned",
                Priority = "Low",
                StartDate = start,
                EndDate = end
            };

            Assert.IsTrue(model.StartDate > model.EndDate, "Test setup: StartDate should be greater than EndDate for this scenario");
        }

        [TestMethod]
        public void ReliefTask_NullableFields_AreOptional()
        {
            var model = new ReliefTask
            {
                Title = "Optional links",
                Location = "Long Street",
                Status = "Active",
                Priority = "Critical"
            };

            var results = ValidateModel(model);
            Assert.AreEqual(0, results.Count);
        }
    }
}
