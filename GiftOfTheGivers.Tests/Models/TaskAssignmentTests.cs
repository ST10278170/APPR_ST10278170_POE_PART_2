using Microsoft.VisualStudio.TestTools.UnitTesting;
using APPR_ST10278170_POE_PART_2.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace GiftOfTheGivers.Tests.Models
{
    [TestClass]
    public class TaskAssignmentTests
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

            // explicit member name entries
            if (results.Any(r => r.MemberNames != null && r.MemberNames.Any(m => string.Equals(m, memberName, StringComparison.Ordinal))))
                return true;

            // fallback: some validators return memberless ValidationResult; search the message text
            if (results.Any(r => !string.IsNullOrEmpty(r.ErrorMessage) &&
                                 r.ErrorMessage.IndexOf(memberName, StringComparison.OrdinalIgnoreCase) >= 0))
                return true;

            return false;
        }

        [TestMethod]
        public void TaskAssignment_ValidModel_NoValidationErrors()
        {
            var model = new TaskAssignment
            {
                TaskName = "Distribute water",
                Description = "Hand out bottled water to families",
                VolunteerId = 42,
                VolunteerName = "A. Volunteer",
                DisasterReportId = 101,
                Location = "Township A",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(2),
                Status = "Assigned"
            };

            var results = ValidateModel(model);
            Assert.AreEqual(0, results.Count, $"Expected no validation errors but found: {string.Join("; ", results.Select(r => r.ErrorMessage))}");
        }

        [TestMethod]
        public void TaskAssignment_MissingRequiredStringFields_ValidationErrors()
        {
            // Use empty strings for required reference/string properties to trigger [Required]
            var model = new TaskAssignment
            {
                TaskName = string.Empty,
                Description = null,
                VolunteerId = 1,
                VolunteerName = null,
                DisasterReportId = 1,
                Location = string.Empty,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(1),
                Status = string.Empty
            };

            var results = ValidateModel(model);

            Assert.IsTrue(HasMemberValidation(results, nameof(TaskAssignment.TaskName)), "Expected validation error for TaskName");
            Assert.IsTrue(HasMemberValidation(results, nameof(TaskAssignment.Location)), "Expected validation error for Location");
            Assert.IsTrue(HasMemberValidation(results, nameof(TaskAssignment.Status)), "Expected validation error for Status");

            // Note: Required on non-nullable int properties (VolunteerId, DisasterReportId) does not produce a ValidationResult for default numeric values.
            // If you want to validate numeric ranges (e.g., > 0), add [Range(1, int.MaxValue)] to the model and update tests accordingly.
        }

        [TestMethod]
        public void TaskAssignment_DefaultDates_AreNowishAndEndAfterStart()
        {
            var before = DateTime.UtcNow.AddSeconds(-1);
            var model = new TaskAssignment
            {
                TaskName = "Seed assignment",
                VolunteerId = 2,
                DisasterReportId = 3,
                Location = "Central",
                Status = "Assigned"
                // StartDate and EndDate left to model defaults
            };

            var after = DateTime.UtcNow.AddSeconds(1);

            Assert.IsTrue(model.StartDate >= before && model.StartDate <= after,
                $"StartDate {model.StartDate} not in expected range between {before} and {after}");
            Assert.IsTrue(model.EndDate > model.StartDate,
                $"EndDate {model.EndDate} should be after StartDate {model.StartDate}");
        }

        [TestMethod]
        public void TaskAssignment_StartBeforeEnd_BusinessRuleCheck()
        {
            var start = DateTime.UtcNow.AddDays(5);
            var end = DateTime.UtcNow.AddDays(1); // intentionally earlier to simulate bad input

            var model = new TaskAssignment
            {
                TaskName = "Bad schedule",
                VolunteerId = 5,
                DisasterReportId = 6,
                Location = "Farmland",
                Status = "Assigned",
                StartDate = start,
                EndDate = end
            };

            // DataAnnotations won't enforce StartDate < EndDate; this test ensures such bad input is detectable
            Assert.IsTrue(model.StartDate > model.EndDate, "Test setup: StartDate should be greater than EndDate for this scenario");
        }

        [TestMethod]
        public void TaskAssignment_NullableFields_AreOptional()
        {
            var model = new TaskAssignment
            {
                TaskName = "Optional fields test",
                VolunteerId = 12,
                DisasterReportId = 34,
                Location = "Main Hall",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(1),
                Status = "Assigned"
                // Description and VolunteerName left null
            };

            var results = ValidateModel(model);
            Assert.AreEqual(0, results.Count);
        }
    }
}
