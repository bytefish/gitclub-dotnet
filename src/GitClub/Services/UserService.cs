// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using GitClub.Database;
using GitClub.Database.Models;
using GitClub.Infrastructure.Authentication;
using GitClub.Infrastructure.Constants;
using GitClub.Infrastructure.Exceptions;
using GitClub.Infrastructure.Logging;
using GitClub.Infrastructure.Outbox;
using GitClub.Infrastructure.Outbox.Messages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GitClub.Services
{
    public class UserService
    {
        private readonly ILogger<UserService> _logger;

        private readonly AclService _aclService;
        private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;

        public UserService(ILogger<UserService> logger, IDbContextFactory<ApplicationDbContext> dbContextFactory, AclService aclService)
        {
            _logger = logger;
            _aclService = aclService;
            _dbContextFactory = dbContextFactory;
        }

        public async Task<User> CreateUserAsync(User user, CurrentUser currentUser, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            bool isAuthorized = currentUser.IsInRole(Roles.Administrator);

            if (!isAuthorized)
            {
                throw new AuthorizationFailedException("Insufficient Permissions to create a new user");
            }

            using var applicationDbContext = await _dbContextFactory
                .CreateDbContextAsync(cancellationToken)
                .ConfigureAwait(false);

            user.LastEditedBy = currentUser.UserId;

            await applicationDbContext
                .AddAsync(user, cancellationToken)
                .ConfigureAwait(false);

            await applicationDbContext
                .SaveChangesAsync(cancellationToken)
                .ConfigureAwait(false);

            return user;
        }

        public async Task DeleteUserByUserIdAsync(int userId, CurrentUser currentUser, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            bool isAuthorized = currentUser.IsInRole(Roles.Administrator);

            if (!isAuthorized)
            {
                throw new AuthorizationFailedException("Insufficient Permissions to create a new user");
            }

            if(currentUser.UserId == userId)
            {
                throw new CannotDeleteOwnUserException 
                { 
                    UserId = currentUser.UserId
                };
            }

            using var applicationDbContext = await _dbContextFactory
                .CreateDbContextAsync(cancellationToken)
                .ConfigureAwait(false);

            // Now we can safely update and delete the data in a transaction
            using (var transaction = await applicationDbContext.Database
                    .BeginTransactionAsync(cancellationToken)
                    .ConfigureAwait(false))
            {
                var userIssueRoles = await applicationDbContext.UserIssueRoles.AsNoTracking()
                    .Where(x => x.UserId == userId)
                    .ToListAsync(cancellationToken);

                var userTeamRoles = await applicationDbContext.UserTeamRoles.AsNoTracking()
                    .Where(x => x.UserId == userId)
                    .ToListAsync(cancellationToken);

                var userRepositoryRoles = await applicationDbContext.UserRepositoryRoles.AsNoTracking()
                    .Where(x => x.UserId == userId)
                    .ToListAsync(cancellationToken);

                var userOrganizationRoles = await applicationDbContext.UserOrganizationRoles.AsNoTracking()
                    .Where(x => x.UserId == userId)
                    .ToListAsync(cancellationToken);

                await applicationDbContext.Organizations
                    .Where(x => x.LastEditedBy == userId)
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.LastEditedBy, Users.GhostUserId));

                await applicationDbContext.OrganizationRoles
                    .Where(x => x.LastEditedBy == userId)
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.LastEditedBy, Users.GhostUserId));

                await applicationDbContext.Repositories
                    .Where(x => x.LastEditedBy == userId)
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.LastEditedBy, Users.GhostUserId));

                await applicationDbContext.RepositoryRoles
                    .Where(x => x.LastEditedBy == userId)
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.LastEditedBy, Users.GhostUserId));

                await applicationDbContext.Teams
                    .Where(x => x.LastEditedBy == userId)
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.LastEditedBy, Users.GhostUserId));

                await applicationDbContext.TeamRoles
                    .Where(x => x.LastEditedBy == userId)
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.LastEditedBy, Users.GhostUserId));

                await applicationDbContext.TeamRepositoryRoles
                    .Where(x => x.LastEditedBy == userId)
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.LastEditedBy, Users.GhostUserId));

                await applicationDbContext.Users
                    .Where(x => x.LastEditedBy == userId)
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.LastEditedBy, Users.GhostUserId));

                await applicationDbContext.UserRepositoryRoles
                    .Where(x => x.LastEditedBy == userId)
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.LastEditedBy, Users.GhostUserId));

                await applicationDbContext.UserOrganizationRoles
                    .Where(x => x.LastEditedBy == userId)
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.LastEditedBy, Users.GhostUserId));

                await applicationDbContext.UserTeamRoles
                    .Where(x => x.LastEditedBy == userId)
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.LastEditedBy, Users.GhostUserId));

                await applicationDbContext.Issues
                    .Where(x => x.LastEditedBy == userId)
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.LastEditedBy, Users.GhostUserId));
                
                await applicationDbContext.UserIssueRoles
                    .Where(x => x.LastEditedBy == userId)
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.LastEditedBy, Users.GhostUserId));

                // We also need to assign the Issue Creator to the GhostUser:
                await applicationDbContext.Issues
                    .Where(x => x.CreatedBy == userId)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(p => p.LastEditedBy, Users.GhostUserId)
                        .SetProperty(p => p.CreatedBy, Users.GhostUserId));

                // Now delete all associations from a User to Organization, Teams and Repositories:
                await applicationDbContext.UserOrganizationRoles.AsNoTracking()
                    .Where(x => x.UserId == userId)
                    .ExecuteDeleteAsync();

                await applicationDbContext.UserTeamRoles.AsNoTracking()
                    .Where(x => x.UserId == userId)
                    .ExecuteDeleteAsync();

                await applicationDbContext.UserRepositoryRoles.AsNoTracking()
                    .Where(x => x.UserId == userId)
                    .ExecuteDeleteAsync();

                await applicationDbContext.UserIssueRoles.AsNoTracking()
                    .Where(x => x.UserId == userId)
                    .ExecuteDeleteAsync();

                var outboxEvent = OutboxEventUtils.Create(new UserDeletedMessage
                {
                    UserId = userId,
                    UserIssueRoles = userIssueRoles
                        .Select(x => new RemovedUserFromIssueMessage { IssueId = x.IssueId, UserId = x.UserId, Role = x.Role })
                        .ToList(),
                    UserOrganizationRoles = userOrganizationRoles
                        .Select(x => new RemovedUserFromOrganizationMessage { OrganizationId = x.OrganizationId, UserId = x.UserId, Role = x.Role })
                        .ToList(),
                    UserRepositoryRoles = userRepositoryRoles
                        .Select(x => new RemovedUserFromRepositoryMessage { RepositoryId = x.RepositoryId, UserId = x.UserId, Role = x.Role })
                        .ToList(),
                    UserTeamRoles = userTeamRoles
                        .Select(x => new RemovedUserFromTeamMessage { TeamId = x.TeamId, UserId = x.UserId, Role = x.Role })
                        .ToList(),
                }, lastEditedBy: currentUser.UserId);

                await applicationDbContext
                    .AddAsync(outboxEvent, cancellationToken)
                    .ConfigureAwait(false);

                await applicationDbContext
                    .SaveChangesAsync(cancellationToken)
                    .ConfigureAwait(false);
                
                await transaction
                    .CommitAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        public async Task<User> GetUserByEmailAsync(string email, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            using var applicationDbContext = await _dbContextFactory
                .CreateDbContextAsync(cancellationToken)
                .ConfigureAwait(false);


            var user = await applicationDbContext.Users.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Email == email, cancellationToken)
                .ConfigureAwait(false);

            if (user == null)
            {
                throw new AuthenticationFailedException();
            }

            return user;
        }

        public async Task<List<Claim>> GetClaimsAsync(string email, string[] roles, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            using var applicationDbContext = await _dbContextFactory
                .CreateDbContextAsync(cancellationToken)
                .ConfigureAwait(false);

            var user = await applicationDbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Email == email, cancellationToken)
                .ConfigureAwait(false);

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