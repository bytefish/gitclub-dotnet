// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using GitClub.Database.Models;
using GitClub.Services;
using GitClub.Infrastructure;

namespace GitClub.Tests.Services
{
    [TestClass]
    public class OrganizationServiceTests : IntegrationTestBase
    {
        protected OrganizationService OrganizationService => GetRequiredService<OrganizationService>();

        [TestMethod]
        public async Task CreateOrganizationAsync_Success()
        {
            var ct = default(CancellationToken);

        }
    }
}
