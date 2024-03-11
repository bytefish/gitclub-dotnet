// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using GitClub.Database;
using GitClub.Database.Models;
using GitClub.Infrastructure.Exceptions;
using GitClub.Infrastructure.Logging;
using Microsoft.EntityFrameworkCore;
using OpenFga.Sdk.Client;
using OpenFga.Sdk.Model;
using RebacExperiments.Server.Api.Infrastructure.Authentication;
using RebacExperiments.Server.Api.Infrastructure.Constants;
using RebacExperiments.Server.Api.Infrastructure.Exceptions;
using RebacExperiments.Server.Api.Infrastructure.Logging;
using RebacExperiments.Server.Database;
using RebacExperiments.Server.Database.Models;
using System.Security.Claims;

namespace GitClub.Services
{
    public class UserService : IUserService
    {
        private readonly ILogger<UserService> _logger;
        private readonly ApplicationDbContext _applicationDbContext;

        public UserService(ILogger<UserService> logger, ApplicationDbContext applicationDbContext)
        {
            _logger = logger;
            _applicationDbContext = applicationDbContext;
        }

        public async Task<List<Claim>> GetClaimsAsync(string email, string[] roles, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            var user = await _applicationDbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Email == email, cancellationToken);

            if (user == null)
            {
                throw new AuthenticationFailedException();
            }

            // Build the Claims for the ClaimsPrincipal
            var claims = CreateClaims(user, roles);

            return claims;
        }

        private List<Claim> CreateClaims(User user, string[] roles)
        {
            _logger.TraceMethodEntry();

            var claims = new List<Claim>();

            claims.Add(new Claim(ClaimTypes.NameIdentifier, user.Email));
            claims.Add(new Claim(ClaimTypes.Email, user.Email));
            claims.Add(new Claim(ClaimTypes.Sid, Convert.ToString(user.Id)));
            claims.Add(new Claim(ClaimTypes.Name, Convert.ToString(user.PreferredName)));

            // Roles:
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            return claims;
        }
    }
}