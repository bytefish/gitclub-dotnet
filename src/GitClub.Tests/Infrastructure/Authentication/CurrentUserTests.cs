// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using GitClub.Infrastructure.Authentication;
using GitClub.Infrastructure.Constants;
using System.Security.Claims;

namespace GitClub.Tests.Infrastructure.Authentication
{
    [TestClass]
    public class CurrentUserTests
    {
        [TestMethod]
        public void IsInRole_CurrentUserIsInRole()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Role, Roles.User),
                new Claim(ClaimTypes.Role, Roles.Administrator)
            };

            var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(claims));

            var currentUser = new CurrentUser
            {
                Principal = claimsPrincipal
            };

            // Act
            var isAdministrator = currentUser.IsInRole(Roles.Administrator);

            // Assert
            Assert.AreEqual(true, isAdministrator);
        }

        [TestMethod]
        public void IsInRole_CurrentUserIsNotInRole()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Role, Roles.User)
            };

            var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(claims));

            var currentUser = new CurrentUser
            {
                Principal = claimsPrincipal
            };

            // Act
            var isAdministrator = currentUser.IsInRole(Roles.Administrator);

            // Assert
            Assert.AreEqual(false, isAdministrator);
        }
    }
}
