using GitClub.Database.Models;
using GitClub.Infrastructure.Constants;
using GitClub.Services;

namespace GitClub.Infrastructure.OpenFga
{
    public static class RelationsExtensions
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

        public static string AsRelation(this TeamRoleEnum source)
        {
            return source switch
            {
                TeamRoleEnum.Member => Relations.Member,
                TeamRoleEnum.Maintainer => Relations.Maintainer,
                _ => throw new ArgumentException($"Could not translate '{source}'", nameof(source))
            };
        }

        public static string AsRelation(this OrganizationRoleEnum source)
        {
            return source switch
            {
                OrganizationRoleEnum.Member => Relations.Member,
                OrganizationRoleEnum.BillingManager => Relations.BillingManager,
                OrganizationRoleEnum.Owner => Relations.Owner,
                _ => throw new ArgumentException($"Could not translate '{source}'", nameof(source))
            };
        }

        public static string AsRelation(this RepositoryRoleEnum source)
        {
            return source switch
            {
                RepositoryRoleEnum.Reader => Relations.Reader,
                RepositoryRoleEnum.Triager => Relations.Triager,
                RepositoryRoleEnum.Writer => Relations.Writer,
                RepositoryRoleEnum.Maintainer => Relations.Maintainer,
                RepositoryRoleEnum.Administrator => Relations.Administrator,
                _ => throw new ArgumentException($"Could not translate '{source}'", nameof(source))
            };
        }

        public static string AsRelation(this BaseRepositoryRoleEnum source)
        {
            return source switch
            {
                BaseRepositoryRoleEnum.RepositoryReader => Relations.RepoReader,
                BaseRepositoryRoleEnum.RepositoryWriter => Relations.RepoWriter,
                BaseRepositoryRoleEnum.RepositoryAdministrator => Relations.RepoAdmin,
                _ => throw new ArgumentException($"Could not translate '{source}'", nameof(source))
            };
        }
    }
}
