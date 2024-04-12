// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using GitClub.Database.Models;
using GitClub.Infrastructure.Constants;

namespace GitClub.Infrastructure.OpenFga
{
    public static class EnumRelationExtensions
    {
        public static string AsRelation(this TeamRoleEnum source)
        {
            return source switch
            {
                TeamRoleEnum.Member => Relations.Member,
                TeamRoleEnum.Maintainer => Relations.Maintainer,
                TeamRoleEnum.Owner => Relations.Owner,
                _ => throw new ArgumentException($"Could not translate '{source}'", nameof(source))
            };
        }

        public static string AsRelation(this IssueRoleEnum source)
        {
            return source switch
            {
                IssueRoleEnum.Owner => Relations.Owner,
                IssueRoleEnum.Creator => Relations.Creator,
                IssueRoleEnum.Assignee => Relations.Assignee,
                IssueRoleEnum.Reader => Relations.Reader,
                IssueRoleEnum.Writer => Relations.Writer,
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
                OrganizationRoleEnum.Administrator => Relations.Administrator,
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
                RepositoryRoleEnum.Owner => Relations.Owner,
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
