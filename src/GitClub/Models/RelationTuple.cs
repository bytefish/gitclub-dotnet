using GitClub.Database.Models;
using GitClub.Infrastructure.OpenFga;

namespace GitClub.Models
{
    public record RelationTuple
    {
        /// <summary>
        /// Gets or sets the Object.
        /// </summary>
        public required string Object { get; set; }

        /// <summary>
        /// Gets or sets the Relation.
        /// </summary>
        public required string Relation { get; set; }

        /// <summary>
        /// Gets or sets the Subject.
        /// </summary>
        public required string Subject { get; set; }

        public static RelationTuple Create<TObjectType, TSubjectType>(TObjectType @object, TSubjectType subject, string relation, string? subjectRelation = null)
            where TObjectType : Entity
            where TSubjectType : Entity
        {
            return new RelationTuple
            {
                Object = ZanzibarFormatters.ToZanzibarNotation(@object),
                Relation = relation,
                Subject = ZanzibarFormatters.ToZanzibarNotation(subject, subjectRelation)
            };
        }
        public static RelationTuple Create<TObjectType, TSubjectType>(int objectId, int subjectId, string relation, string? subjectRelation = null)
            where TObjectType : Entity
            where TSubjectType : Entity
        {
            return new RelationTuple
            {
                Object = ZanzibarFormatters.ToZanzibarNotation<TObjectType>(objectId),
                Relation = relation,
                Subject = ZanzibarFormatters.ToZanzibarNotation<TSubjectType>(subjectId, subjectRelation)
            };
        }
    }
}
