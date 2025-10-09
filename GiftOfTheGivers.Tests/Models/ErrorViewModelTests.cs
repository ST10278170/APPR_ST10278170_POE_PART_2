using Microsoft.VisualStudio.TestTools.UnitTesting;
using APPR_ST10278170_POE_PART_2.Models;
using System;

namespace GiftOfTheGivers.Tests.Models
{
    [TestClass]
    public class ErrorViewModelTests
    {
        [TestMethod]
        public void ErrorViewModel_DefaultTimestamp_IsNowish()
        {
            var before = DateTime.Now.AddSeconds(-1);
            var model = new ErrorViewModel
            {
                RequestId = "R1",
                ErrorMessage = "err",
                StackTrace = "st"
            };
            var after = DateTime.Now.AddSeconds(1);

            Assert.IsTrue(model.Timestamp >= before && model.Timestamp <= after,
                $"Timestamp {model.Timestamp} not in expected range between {before} and {after}");
        }

        [TestMethod]
        public void ErrorViewModel_ShowRequestId_WhenRequestIdSet_ReturnsTrue()
        {
            var model = new ErrorViewModel
            {
                RequestId = "REQ-123",
                ErrorMessage = "err",
                StackTrace = "st"
            };

            Assert.IsTrue(model.ShowRequestId);
        }

        [TestMethod]
        public void ErrorViewModel_ShowRequestId_WhenRequestIdEmpty_ReturnsFalse()
        {
            var model = new ErrorViewModel
            {
                RequestId = string.Empty,
                ErrorMessage = "err",
                StackTrace = "st"
            };

            Assert.IsFalse(model.ShowRequestId);
        }

        [TestMethod]
        public void ErrorViewModel_CanSetAndGetProperties()
        {
            var model = new ErrorViewModel
            {
                RequestId = "R-42",
                ErrorMessage = "Something went wrong",
                StackTrace = "stack trace here"
            };

            Assert.AreEqual("R-42", model.RequestId);
            Assert.AreEqual("Something went wrong", model.ErrorMessage);
            Assert.AreEqual("stack trace here", model.StackTrace);

            // mutate and re-check
            model.RequestId = "R-99";
            model.ErrorMessage = "Different";
            model.StackTrace = "different stack";

            Assert.AreEqual("R-99", model.RequestId);
            Assert.AreEqual("Different", model.ErrorMessage);
            Assert.AreEqual("different stack", model.StackTrace);
        }
    }
}
