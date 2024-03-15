// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using GitClub.Database.Models;

namespace GitClub.Infrastructure.OpenFga
{
    public static class ZanzibarFormatters
    {
        public static string ToZanzibarNotation<TEntity>(int? id, string? relation = null)
            where TEntity : Entity
        {
            return ToZanzibarNotation(typeof(TEntity).Name, id, relation);
        }

        public static string ToZanzibarNotation<TEntity>(TEntity entity, string? relation = null)
            where TEntity : Entity
        {
            return ToZanzibarNotation(typeof(TEntity).Name, entity.Id, relation);
        }

        public static string ToZanzibarNotation(string type, int? id, string? relation = null)
        {
            var strId = id == null ? "" : id.ToString();

            if (string.IsNullOrWhiteSpace(relation))
            {
                return $"{type}:{strId}";
            }

            return $"{type}:{strId}#{relation}";
        }

        public static (string Type, int Id, string? relation) FromZanzibarNotation(string s)
        {
            if (s.Contains('#'))
            {
                return FromZanzibarNotationWithRelation(s);
            }

            return FromZanzibarNotationWithoutRelation(s);
        }

        public static (string Type, int Id, string? relation) FromZanzibarNotationWithoutRelation(string s)
        {
            var parts = s.Split(':');

            if (parts.Length != 2)
            {
                throw new InvalidOperationException($"'{s}' is not a valid string. Expected a Type and Id, such as 'User:1'");
            }

            var type = parts[0];

            if (!int.TryParse(parts[1], out var id))
            {
                throw new InvalidOperationException($"'{s}' is not a valid string. The Id '{parts[1]}' is not a valid integer");
            }

            return (type, id, null);
        }

        public static (string Type, int Id, string? relation) FromZanzibarNotationWithRelation(string s)
        {
            var parts = s.Split("#");

            if (parts.Length != 2)
            {
                throw new InvalidOperationException("Invalid Userset String, expected format 'Type:Id#Relation''");
            }

            var innerParts = parts[0].Split(":");

            if (innerParts.Length != 2)
            {
                throw new InvalidOperationException("Invalid Userset String, expected format 'Type:Id#Relation'");
            }

            var type = innerParts[0];
            var relation = parts[1];

            if (!int.TryParse(innerParts[1], out var id))
            {
                throw new InvalidOperationException($"Invalid Userset String, the Id '{innerParts[1]}' is not a valid integer");
            }

            return (type, id, relation);
        }
    }
}
