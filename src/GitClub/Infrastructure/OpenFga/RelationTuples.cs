// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using GitClub.Database.Models;
using GitClub.Models;

namespace GitClub.Infrastructure.OpenFga
{
    /// <summary>
    /// A utility class to provide application-specific methods to prevent using weakly-typed strings for relations, 
    /// but use the applications enumerations such as a <see cref="BaseRepositoryRoleEnum"/> to translate to make the 
    /// code easier to read.
    /// </summary>
    public static class RelationTuples
    {
        public static RelationTuple Create<TObjectType, TSubjectType>(TObjectType @object, TSubjectType subject, BaseRepositoryRoleEnum role, string? subjectRelation = null)
            where TObjectType : Entity
            where TSubjectType : Entity
        {
            return RelationTuple.Create(@object, subject, role.AsRelation(), subjectRelation);
        }

        public static RelationTuple Create<TObjectType, TSubjectType>(int objectId, int subjectId, BaseRepositoryRoleEnum role, string? subjectRelation = null)
            where TObjectType : Entity
            where TSubjectType : Entity
        {
            return RelationTuple.Create<TObjectType, TSubjectType>(objectId, subjectId, role.AsRelation(), subjectRelation);
        }

        public static RelationTuple Create<TObjectType, TSubjectType>(TObjectType @object, TSubjectType subject, TeamRoleEnum role, string? subjectRelation = null)
            where TObjectType : Entity
            where TSubjectType : Entity
        {
            return RelationTuple.Create(@object, subject, role.AsRelation(), subjectRelation);
        }
        
        public static RelationTuple Create<TObjectType, TSubjectType>(TObjectType @object, TSubjectType subject, IssueRoleEnum role, string? subjectRelation = null)
            where TObjectType : Entity
            where TSubjectType : Entity
        {
            return RelationTuple.Create(@object, subject, role.AsRelation(), subjectRelation);
        }

        public static RelationTuple Create<TObjectType, TSubjectType>(int objectId, int subjectId, TeamRoleEnum role, string? subjectRelation = null)
            where TObjectType : Entity
            where TSubjectType : Entity
        {
            return RelationTuple.Create<TObjectType, TSubjectType>(objectId, subjectId, role.AsRelation(), subjectRelation);
        }

        public static RelationTuple Create<TObjectType, TSubjectType>(int objectId, int subjectId, IssueRoleEnum role, string? subjectRelation = null)
            where TObjectType : Entity
            where TSubjectType : Entity
        {
            return RelationTuple.Create<TObjectType, TSubjectType>(objectId, subjectId, role.AsRelation(), subjectRelation);
        }

        public static RelationTuple Create<TObjectType, TSubjectType>(TObjectType @object, TSubjectType subject, OrganizationRoleEnum role, string? subjectRelation = null)
            where TObjectType : Entity
            where TSubjectType : Entity
        {
            return RelationTuple.Create(@object, subject, role.AsRelation(), subjectRelation);
        }

        public static RelationTuple Create<TObjectType, TSubjectType>(int objectId, int subjectId, OrganizationRoleEnum role, string? subjectRelation = null)
            where TObjectType : Entity
            where TSubjectType : Entity
        {
            return RelationTuple.Create<TObjectType, TSubjectType>(objectId, subjectId, role.AsRelation(), subjectRelation);
        }

        public static RelationTuple Create<TObjectType, TSubjectType>(TObjectType @object, TSubjectType subject, RepositoryRoleEnum role, string? subjectRelation = null)
            where TObjectType : Entity
            where TSubjectType : Entity
        {
            return RelationTuple.Create(@object, subject, role.AsRelation(), subjectRelation);
        }

        public static RelationTuple Create<TObjectType, TSubjectType>(int objectId, int subjectId, RepositoryRoleEnum role, string? subjectRelation = null)
            where TObjectType : Entity
            where TSubjectType : Entity
        {
            return RelationTuple.Create<TObjectType, TSubjectType>(objectId, subjectId, role.AsRelation(), subjectRelation);
        }
    }
}
