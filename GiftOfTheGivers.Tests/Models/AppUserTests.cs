using Microsoft.VisualStudio.TestTools.UnitTesting;
using APPR_ST10278170_POE_PART_2.Models;

namespace GiftOfTheGivers.Tests.Models
{
    [TestClass]
    public class AppUserTests
    {
        [TestMethod]
        public void AppUser_DefaultValues_AreAsExpected()
        {
            var user = new AppUser();

            Assert.AreEqual(0, user.Id, "Default Id should be 0");
            Assert.AreEqual(string.Empty, user.Username, "Default Username should be empty string");
            Assert.AreEqual(string.Empty, user.PasswordHash, "Default PasswordHash should be empty string");
            Assert.AreEqual("User", user.Role, "Default Role should be 'User'");
        }

        [TestMethod]
        public void AppUser_CanSetAndGetProperties()
        {
            var user = new AppUser
            {
                Id = 42,
                Username = "alice",
                PasswordHash = "hashedpwd",
                Role = "Admin"
            };

            Assert.AreEqual(42, user.Id);
            Assert.AreEqual("alice", user.Username);
            Assert.AreEqual("hashedpwd", user.PasswordHash);
            Assert.AreEqual("Admin", user.Role);
        }

        [TestMethod]
        public void AppUser_TwoInstances_WithSameValues_AreEquivalentByPropertyValues()
        {
            var a = new AppUser
            {
                Id = 1,
                Username = "bob",
                PasswordHash = "h1",
                Role = "User"
            };

            var b = new AppUser
            {
                Id = 1,
                Username = "bob",
                PasswordHash = "h1",
                Role = "User"
            };

            Assert.IsTrue(AreEquivalentByProperties(a, b));
        }

        [TestMethod]
        public void AppUser_ModifyingOneInstance_DoesNotAffectAnother()
        {
            var a = new AppUser { Id = 1, Username = "bob" };
            var b = new AppUser { Id = 1, Username = "bob" };

            a.Username = "charlie";

            Assert.AreEqual("charlie", a.Username);
            Assert.AreEqual("bob", b.Username);
        }

        private static bool AreEquivalentByProperties(AppUser x, AppUser y)
        {
            if (x is null && y is null) return true;
            if (x is null || y is null) return false;
            return x.Id == y.Id
                && x.Username == y.Username
                && x.PasswordHash == y.PasswordHash
                && x.Role == y.Role;
        }
    }
}
