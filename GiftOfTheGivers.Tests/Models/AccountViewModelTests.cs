using Microsoft.VisualStudio.TestTools.UnitTesting;
using APPR_ST10278170_POE_PART_2.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace GiftOfTheGivers.Tests.Models
{
    [TestClass]
    public class AccountViewModelTests
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
        public void AccountViewModel_ValidLoginModel_NoValidationErrors()
        {
            var model = new AccountViewModel
            {
                Username = "user1",
                Password = "Secret123!",
                ConfirmPassword = "Secret123!",
                IsRegistering = false
            };

            var results = ValidateModel(model);
            Assert.AreEqual(0, results.Count);
        }

        [TestMethod]
        public void AccountViewModel_MissingUsername_ValidationError()
        {
            var model = new AccountViewModel
            {
                Username = string.Empty,
                Password = "Secret123!",
                ConfirmPassword = "Secret123!",
                IsRegistering = false
            };

            var results = ValidateModel(model);
            Assert.IsTrue(results.Any(r => r.MemberNames.Contains(nameof(AccountViewModel.Username))),
                "Expected validation error for Username");
        }

        [TestMethod]
        public void AccountViewModel_MissingPassword_ValidationError()
        {
            var model = new AccountViewModel
            {
                Username = "user1",
                Password = string.Empty,
                ConfirmPassword = string.Empty,
                IsRegistering = false
            };

            var results = ValidateModel(model);
            Assert.IsTrue(results.Any(r => r.MemberNames.Contains(nameof(AccountViewModel.Password))),
                "Expected validation error for Password");
        }

        [TestMethod]
        public void AccountViewModel_Registering_PasswordsDoNotMatch_ValidationError()
        {
            var model = new AccountViewModel
            {
                Username = "user1",
                Password = "Secret123!",
                ConfirmPassword = "Different!",
                IsRegistering = true
            };

            var results = ValidateModel(model);
            Assert.IsTrue(results.Any(r => r.MemberNames.Contains(nameof(AccountViewModel.ConfirmPassword))),
                "Expected validation error for ConfirmPassword when passwords do not match");
        }

        [TestMethod]
        public void AccountViewModel_Registering_PasswordsMatch_NoValidationError()
        {
            var model = new AccountViewModel
            {
                Username = "user1",
                Password = "Secret123!",
                ConfirmPassword = "Secret123!",
                IsRegistering = true
            };

            var results = ValidateModel(model);
            Assert.AreEqual(0, results.Count);
        }

        [TestMethod]
        public void AccountViewModel_DefaultReturnUrl_IsDashboard()
        {
            var model = new AccountViewModel();
            Assert.AreEqual("/Dashboard/Index", model.ReturnUrl);
        }
    }
}
