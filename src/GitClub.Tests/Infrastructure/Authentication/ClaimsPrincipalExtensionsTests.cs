// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using GitClub.Infrastructure.Authentication;
using System.Security.Claims;

namespace GitClub.Tests.Infrastructure.Authentication
{
    [TestClass]
    public class ClaimsPrincipalExtensionsTests
    {
        [TestMethod]
        public void GetUserId_Success()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Sid, "34")
            };

            var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(claims));

            // Act
            var userId = claimsPrincipal.GetUserId();

            // Assert
            Assert.AreEqual(34, userId);
        }

        [TestMethod]
        public void GetUserId_ThrowsIfNotFound()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "Philipp")
            };

            var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(claims));

            // Act
            Exception? caught = null;

            try
            {
                var _ = claimsPrincipal.GetUserId();
            } 
            catch(Exception e)
            {
                caught = e;
            }

            // Assert
            Assert.IsNotNull(caught);

            Assert.AreEqual("No UserID found for ClaimsPrincipal", caught.Message);
        }

        [TestMethod]
        public void GetUserId_ThrowsIfConversionFailed()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Sid, "Philipp")
            };

            var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(claims));

            // Act
            Exception? caught = null;

            try
            {
                var _ = claimsPrincipal.GetUserId();
            }
            catch (Exception e)
            {
                caught = e;
            }

            // Assert
            Assert.IsNotNull(caught);

            Assert.AreEqual("UserID could not be converted to an Int32", caught.Message);
        }
    }
}
