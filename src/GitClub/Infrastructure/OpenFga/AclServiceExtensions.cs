// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using GitClub.Database.Models;
using GitClub.Services;

namespace GitClub.Infrastructure.OpenFga
{
    public static class AclServiceExtensions
    {
        public static async Task<bool> CheckUserObjectAsync<TObjectType>(this AclService aclService, int userId, int objectId, OrganizationRoleEnum role, CancellationToken cancellationToken)
    where TObjectType : Entity
        {
            var relation = role.AsRelation();

            var allowed = await aclService
                .CheckObjectAsync<TObjectType, User>(objectId, relation, userId, cancellationToken)
                .ConfigureAwait(false);

            return allowed;
        }

        public static async Task<bool> CheckUserObjectAsync<TObjectType>(this AclService aclService, int userId, int objectId, RepositoryRoleEnum role, CancellationToken cancellationToken)
            where TObjectType : Entity
        {

            var relation = role.AsRelation();

            var allowed = await aclService
                .CheckObjectAsync<TObjectType, User>(objectId, relation, userId, cancellationToken)
                .ConfigureAwait(false);

            return allowed;
        }

        public static async Task<bool> CheckUserObjectAsync<TObjectType>(this AclService aclService, int userId, int objectId, BaseRepositoryRoleEnum role, CancellationToken cancellationToken)
            where TObjectType : Entity
        {
            var relation = role.AsRelation();

            var allowed = await aclService
                .CheckObjectAsync<TObjectType, User>(objectId, relation, userId, cancellationToken)
                .ConfigureAwait(false);

            return allowed;
        }

        public static async Task<bool> CheckUserObjectAsync<TObjectType>(this AclService aclService, int userId, int objectId, TeamRoleEnum role, CancellationToken cancellationToken)
            where TObjectType : Entity
        {
            var relation = role.AsRelation();

            var allowed = await aclService
                .CheckObjectAsync<TObjectType, User>(objectId, relation, userId, cancellationToken)
                .ConfigureAwait(false);

            return allowed;
        }

        public static async Task<bool> CheckUserObjectAsync<TObjectType>(this AclService aclService, int userId, int objectId, IssueRoleEnum role, CancellationToken cancellationToken)
            where TObjectType : Entity
        {
            var relation = role.AsRelation();

            var allowed = await aclService
                .CheckObjectAsync<TObjectType, User>(objectId, relation, userId, cancellationToken)
                .ConfigureAwait(false);

            return allowed;
        }
    }
}
