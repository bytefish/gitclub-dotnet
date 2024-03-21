
using GitClub.Database.Models;
using GitClub.Infrastructure.OpenFga;

namespace GitClub.Models
{
    /// <summary>
    /// A Relation Tuple (Object, Relation, Subject), using a Zanzibar Notation and simplifying 
    /// the creation of Relation Tuples from an <see cref="Entity"/>.
    /// </summary>
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

        /// <summary>
        /// Creates a new <see cref="RelationTuple">.
        /// </summary>
        /// <typeparam name="TObjectType">Type of the Object</typeparam>
        /// <typeparam name="TSubjectType">Type of the Subject</typeparam>
        /// <param name="object">Object, for example a Repository</param>
        /// <param name="subject">Subject, most probably a User</param>
        /// <param name="relation">Relation between Object and Subject</param>
        /// <param name="subjectRelation">The Subject relation, to model something like Organization#member</param>
        /// <returns>The Relation Tuple in Zanzibar Notation</returns>
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

        /// <summary>
        /// Creates a new <see cref="RelationTuple">.
        /// </summary>
        /// <typeparam name="TObjectType">Type of the Object</typeparam>
        /// <typeparam name="TSubjectType">Type of the Subject</typeparam>
        /// <param name="objectId">ID of the Object, for example a Repository ID</param>
        /// <param name="subjectId">Subject ID, most probably a User ID</param>
        /// <param name="relation">Relation between Object and Subject</param>
        /// <param name="subjectRelation">The Subject relation, to model something like Organization#member</param>
        /// <returns>The Relation Tuple in Zanzibar Notation</returns>
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
