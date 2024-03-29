﻿// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using GitClub.Database;
using GitClub.Database.Models;
using GitClub.Infrastructure.Authentication;
using GitClub.Infrastructure.Constants;
using GitClub.Infrastructure.Exceptions;
using GitClub.Infrastructure.Logging;
using GitClub.Infrastructure.OpenFga;
using GitClub.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GitClub.Services
{
    public class UserService
    {
        private readonly ILogger<UserService> _logger;

        private readonly AclService _aclService;
        private readonly ApplicationDbContext _applicationDbContext;

        public UserService(ILogger<UserService> logger, AclService aclService, ApplicationDbContext applicationDbContext)
        {
            _logger = logger;
            _aclService = aclService;
            _applicationDbContext = applicationDbContext;
        }

        public async Task<User> CreateUserAsync(User user, CurrentUser currentUser, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            bool isAuthorized = currentUser.IsInRole(Roles.Administrator);

            if (!isAuthorized)
            {
                throw new AuthorizationFailedException("Insufficient Permissions to create a new user");
            }

            user.LastEditedBy = currentUser.UserId;

            await _applicationDbContext
                .AddAsync(user, cancellationToken)
                .ConfigureAwait(false);

            await _applicationDbContext
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

            // Get all related data to delete:
            var createdIssues = await _applicationDbContext.Issues.AsNoTracking()
                .Where(x => x.CreatedBy == userId)
                .ToListAsync(cancellationToken);

            var userTeamRoles = await _applicationDbContext.UserTeamRoles.AsNoTracking()
                .Where(x => x.UserId == userId)
                .ToListAsync(cancellationToken);

            var userRepositoryRoles = await _applicationDbContext.UserRepositoryRoles.AsNoTracking()
                .Where(x => x.UserId == userId)
                .ToListAsync(cancellationToken);

            var userOrganizationRoles = await _applicationDbContext.UserOrganizationRoles.AsNoTracking()
                .Where(x => x.UserId == userId)
                .ToListAsync(cancellationToken);

            // Now we can safely update and delete the data in a transaction
            using (var transaction =
                await _applicationDbContext.Database
                    .BeginTransactionAsync(cancellationToken)
                    .ConfigureAwait(false))
            {
                // Update all LastEditedBy References to the Ghost User
                await _applicationDbContext.Organizations
                    .Where(x => x.LastEditedBy == userId)
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.LastEditedBy, Users.GhostUserId));

                await _applicationDbContext.OrganizationRoles
                    .Where(x => x.LastEditedBy == userId)
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.LastEditedBy, Users.GhostUserId));

                await _applicationDbContext.Repositories
                    .Where(x => x.LastEditedBy == userId)
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.LastEditedBy, Users.GhostUserId));

                await _applicationDbContext.RepositoryRoles
                    .Where(x => x.LastEditedBy == userId)
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.LastEditedBy, Users.GhostUserId));

                await _applicationDbContext.Teams
                    .Where(x => x.LastEditedBy == userId)
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.LastEditedBy, Users.GhostUserId));

                await _applicationDbContext.TeamRoles
                    .Where(x => x.LastEditedBy == userId)
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.LastEditedBy, Users.GhostUserId));

                await _applicationDbContext.TeamRepositoryRoles
                    .Where(x => x.LastEditedBy == userId)
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.LastEditedBy, Users.GhostUserId));

                await _applicationDbContext.Users
                    .Where(x => x.LastEditedBy == userId)
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.LastEditedBy, Users.GhostUserId));

                await _applicationDbContext.UserRepositoryRoles
                    .Where(x => x.LastEditedBy == userId)
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.LastEditedBy, Users.GhostUserId));

                await _applicationDbContext.UserOrganizationRoles
                    .Where(x => x.LastEditedBy == userId)
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.LastEditedBy, Users.GhostUserId));

                await _applicationDbContext.UserTeamRoles
                    .Where(x => x.LastEditedBy == userId)
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.LastEditedBy, Users.GhostUserId));

                await _applicationDbContext.Issues
                    .Where(x => x.LastEditedBy == userId)
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.LastEditedBy, Users.GhostUserId));

                // We also need to assign the Issue Creator to the GhostUser:
                await _applicationDbContext.Issues
                    .Where(x => x.CreatedBy == userId)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(p => p.LastEditedBy, Users.GhostUserId)
                        .SetProperty(p => p.CreatedBy, Users.GhostUserId));

                // Now delete all associations from a User to Organization, Teams and Repositories:
                await _applicationDbContext.UserOrganizationRoles.AsNoTracking()
                    .Where(x => x.UserId == userId)
                    .ExecuteDeleteAsync();

                await _applicationDbContext.UserTeamRoles.AsNoTracking()
                    .Where(x => x.UserId == userId)
                    .ExecuteDeleteAsync();

                await _applicationDbContext.UserRepositoryRoles.AsNoTracking()
                    .Where(x => x.UserId == userId)
                    .ExecuteDeleteAsync();

                await transaction
                    .CommitAsync(cancellationToken)
                    .ConfigureAwait(false);
            }

            // And delete all tuples for the user in Zanzibar
            List<RelationTuple> tuplesToDelete = [];

            foreach (var createdIssue in createdIssues)
            {
                var tuple = RelationTuple.Create<Issue, User>(createdIssue.Id, createdIssue.CreatedBy, Relations.Creator);

                tuplesToDelete.Add(tuple);
            }

            foreach (var userOrganizationRole in userOrganizationRoles)
            {
                var tuple = RelationTuples.Create<Organization, User>(userOrganizationRole.OrganizationId, userOrganizationRole.UserId, userOrganizationRole.Role);

                tuplesToDelete.Add(tuple);
            }

            foreach (var userTeamRole in userTeamRoles)
            {
                var tuple = RelationTuples.Create<Team, User>(userTeamRole.TeamId, userTeamRole.UserId, userTeamRole.Role);

                tuplesToDelete.Add(tuple);
            }

            foreach (var userRepositoryRole in userRepositoryRoles)
            {
                var tuple = RelationTuples.Create<Repository, User>(userRepositoryRole.RepositoryId, userRepositoryRole.UserId, userRepositoryRole.Role);

                tuplesToDelete.Add(tuple);
            }

            await _aclService
                .DeleteRelationshipsAsync(tuplesToDelete, cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<User> GetUserByEmailAsync(string email, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            var user = await _applicationDbContext.Users.AsNoTracking()
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

            var user = await _applicationDbContext.Users
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