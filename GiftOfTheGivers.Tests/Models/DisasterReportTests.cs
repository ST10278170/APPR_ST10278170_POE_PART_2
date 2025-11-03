using Microsoft.VisualStudio.TestTools.UnitTesting;
using APPR_ST10278170_POE_PART_2.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace GiftOfTheGivers.Tests.Models
{
    [TestClass]
    public class DisasterReportTests
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

        [TestMethod]
        public void DisasterReport_ValidModel_NoValidationErrors()
        {
            var model = new DisasterReport
            {
                Location = "Cape Town",
                DisasterType = "Flood",
                Description = "River overflowed after heavy rains",
                DateReported = DateTime.Now,
                Severity = "High",
                ReliefRequired = "Food, water, blankets",
                ReporterName = "Field Agent 1",
                Status = "Pending",
                IsVerified = false
            };

            var results = ValidateModel(model);
            Assert.AreEqual(0, results.Count);
        }

        [TestMethod]
        public void DisasterReport_MissingRequiredFields_ValidationErrors()
        {
            var model = new DisasterReport
            {
                // Missing Location and DisasterType
                Description = "desc",
                Severity = "High",
                Status = "Pending"
            };

            var results = ValidateModel(model);
            Assert.IsTrue(results.Any(r => r.MemberNames.Contains(nameof(DisasterReport.Location))),
                "Expected validation error for Location");
            Assert.IsTrue(results.Any(r => r.MemberNames.Contains(nameof(DisasterReport.DisasterType))),
                "Expected validation error for DisasterType");
        }

        [TestMethod]
        public void DisasterReport_StringLengthExceeded_ValidationError()
        {
            var longLocation = new string('L', 101); // Location max 100
            var longDisasterType = new string('D', 51); // DisasterType max 50
            var longDescription = new string('X', 501); // Description max 500
            var model = new DisasterReport
            {
                Location = longLocation,
                DisasterType = longDisasterType,
                Description = longDescription,
                Severity = "High",
                Status = "Pending"
            };

            var results = ValidateModel(model);
            Assert.IsTrue(results.Any(r => r.MemberNames.Contains(nameof(DisasterReport.Location))),
                "Expected Location string-length validation error");
            Assert.IsTrue(results.Any(r => r.MemberNames.Contains(nameof(DisasterReport.DisasterType))),
                "Expected DisasterType string-length validation error");
            Assert.IsTrue(results.Any(r => r.MemberNames.Contains(nameof(DisasterReport.Description))),
                "Expected Description string-length validation error");
        }

        [TestMethod]
        public void DisasterReport_Defaults_AreSetCorrectly()
        {
            var before = DateTime.Now;
            var model = new DisasterReport();
            var after = DateTime.Now;

            // DateReported default should be between before and after (now-ish)
            Assert.IsTrue(model.DateReported >= before && model.DateReported <= after,
                $"DateReported default not in expected range: {model.DateReported} not between {before} and {after}");

            Assert.AreEqual("Moderate", model.Severity, "Default Severity should be 'Moderate'");
            Assert.AreEqual("Pending", model.Status, "Default Status should be 'Pending'");
            Assert.IsFalse(model.IsVerified, "Default IsVerified should be false");
        }

        [TestMethod]
        public void DisasterReport_StatusAndSeverity_MaxLengths()
        {
            var longSeverity = new string('S', 51); // Severity max 50
            var longStatus = new string('T', 31); // Status max 30
            var model = new DisasterReport
            {
                Location = "X",
                DisasterType = "Y",
                Severity = longSeverity,
                Status = longStatus
            };

            var results = ValidateModel(model);
            Assert.IsTrue(results.Any(r => r.MemberNames.Contains(nameof(DisasterReport.Severity))),
                "Expected Severity string-length validation error");
            Assert.IsTrue(results.Any(r => r.MemberNames.Contains(nameof(DisasterReport.Status))),
                "Expected Status string-length validation error");
        }
    }
}
