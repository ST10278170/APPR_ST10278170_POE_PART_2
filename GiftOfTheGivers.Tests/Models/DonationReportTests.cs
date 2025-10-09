using Microsoft.VisualStudio.TestTools.UnitTesting;
using APPR_ST10278170_POE_PART_2.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace GiftOfTheGivers.Tests.Models
{
    [TestClass]
    public class DonationReportTests
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
        public void DonationReport_ValidMoneyDonation_NoValidationErrors()
        {
            var model = new DonationReport
            {
                DonorName = "Alice",
                DonationType = "Money",
                Amount = 100.50m,
                DateDonated = DateTime.UtcNow
            };

            var results = ValidateModel(model);
            Assert.AreEqual(0, results.Count);
        }

        [TestMethod]
        public void DonationReport_ValidSupplyDonation_NoValidationErrors()
        {
            var model = new DonationReport
            {
                DonorName = "Bob",
                DonationType = "Supplies",
                ResourceType = "Blankets",
                Quantity = 10,
                DateDonated = DateTime.UtcNow
            };

            var results = ValidateModel(model);
            Assert.AreEqual(0, results.Count);
        }

        [TestMethod]
        public void DonationReport_MissingRequiredFields_ValidationErrors()
        {
            // C# required properties must be set at compile-time.
            // Use empty strings to trigger DataAnnotations [Required] while satisfying the compiler.
            var model = new DonationReport
            {
                DonorName = string.Empty,
                DonationType = string.Empty,
                Amount = 50m,
                DateDonated = DateTime.UtcNow
            };

            var results = ValidateModel(model);
            Assert.IsTrue(results.Any(r => r.MemberNames.Contains(nameof(DonationReport.DonorName))),
                "Expected validation error for DonorName");
            Assert.IsTrue(results.Any(r => r.MemberNames.Contains(nameof(DonationReport.DonationType))),
                "Expected validation error for DonationType");
        }

        [TestMethod]
        public void DonationReport_AmountMustBeGreaterThanZero_ValidationError()
        {
            var model = new DonationReport
            {
                DonorName = "Charlie",
                DonationType = "Money",
                Amount = 0m,
                DateDonated = DateTime.UtcNow
            };

            var results = ValidateModel(model);
            Assert.IsTrue(results.Any(r => r.MemberNames.Contains(nameof(DonationReport.Amount))),
                "Expected validation error for Amount when zero or negative");
        }

        [TestMethod]
        public void DonationReport_QuantityMustBeNonNegative_ValidationError()
        {
            var model = new DonationReport
            {
                DonorName = "Dana",
                DonationType = "Supplies",
                Quantity = -5,
                DateDonated = DateTime.UtcNow
            };

            var results = ValidateModel(model);
            Assert.IsTrue(results.Any(r => r.MemberNames.Contains(nameof(DonationReport.Quantity))),
                "Expected validation error for Quantity when negative");
        }

        [TestMethod]
        public void DonationReport_DateDonated_DefaultsToNowUtc()
        {
            var before = DateTime.UtcNow.AddSeconds(-1);
            var model = new DonationReport
            {
                DonorName = "Eve",
                DonationType = "Services"
                // DateDonated intentionally left to default
            };
            var after = DateTime.UtcNow.AddSeconds(1);

            Assert.IsTrue(model.DateDonated >= before && model.DateDonated <= after,
                $"DateDonated default not in expected UTC range: {model.DateDonated}");
        }

        [TestMethod]
        public void DonationReport_NullableFields_AreOptional()
        {
            var model = new DonationReport
            {
                DonorName = "Fay",
                DonationType = "Supplies",
                // Amount, ResourceType, Quantity, TargetLocation, Notes left null
                DateDonated = DateTime.UtcNow
            };

            var results = ValidateModel(model);
            Assert.AreEqual(0, results.Count);
        }
    }
}
