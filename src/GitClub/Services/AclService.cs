﻿// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using OpenFga.Sdk.Client.Model;
using OpenFga.Sdk.Client;
using GitClub.Database;
using GitClub.Database.Models;
using Microsoft.EntityFrameworkCore;
using GitClub.Models;
using GitClub.Infrastructure.Logging;

namespace GitClub.Services
{
    public class AclService
    {
        private readonly ILogger<AclService> _logger;

        private readonly OpenFgaClient _openFgaClient;

        private readonly ApplicationDbContext _applicationDbContext;

        public AclService(ILogger<AclService> logger, OpenFgaClient openFgaClient, ApplicationDbContext applicationDbContext)
        {
            _logger = logger;
            _openFgaClient = openFgaClient;
            _applicationDbContext = applicationDbContext;
        }

        public async Task<bool> CheckObjectAsync<TObjectType, TSubjectType>(int objectId, string relation, int subjectId, CancellationToken cancellationToken)
            where TObjectType : Entity
            where TSubjectType : Entity
        {
            _logger.TraceMethodEntry();

            var body = new ClientCheckRequest
            {
                Object = ToZanzibarNotation<TObjectType>(objectId),
                User = ToZanzibarNotation<TSubjectType>(subjectId),
                Relation = relation,
            };

            var response = await _openFgaClient
                .Check(body, null, cancellationToken)
                .ConfigureAwait(false);

            if (response == null)
            {
                throw new InvalidOperationException("No Response received");
            }

            if (response.Allowed == null)
            {
                return false;
            }

            return response.Allowed.Value;
        }

        public async Task<bool> CheckUserObjectAsync<TObjectType>(int userId, int objectId, string relation, CancellationToken cancellationToken)
            where TObjectType : Entity
        {
            var allowed = await CheckObjectAsync<TObjectType, User>(objectId, relation, userId, cancellationToken).ConfigureAwait(false);

            return allowed;
        }

        public async Task<bool> CheckUserObjectAsync<TObjectType>(int userId, TObjectType @object, string relation, CancellationToken cancellationToken)
            where TObjectType : Entity
        {
            var allowed = await CheckObjectAsync<TObjectType, User>(@object.Id, relation, userId, cancellationToken).ConfigureAwait(false);

            return allowed;
        }

        public async Task<List<TObjectType>> ListObjectsAsync<TObjectType, TSubjectType>(int subjectId, string relation, CancellationToken cancellationToken)
            where TObjectType : Entity
            where TSubjectType : Entity
        {
            _logger.TraceMethodEntry();

            var body = new ClientListObjectsRequest
            {
                Type = typeof(TObjectType).Name,
                User = ToZanzibarNotation<TSubjectType>(subjectId),
                Relation = relation
            };

            var response = await _openFgaClient
                .ListObjects(body, null, cancellationToken)
                .ConfigureAwait(false);

            if (response == null)
            {
                throw new InvalidOperationException("No Response received");
            }

            if (response.Objects == null)
            {
                return [];
            }

            var objectIds = response.Objects
                .Select(x => FromZanzibarNotation(x))
                .Select(x => x.Id)
                .ToArray();

            var entities = await _applicationDbContext.Set<TObjectType>()
                .AsNoTracking()
                .Where(x => objectIds.Contains(x.Id))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            return entities;
        }

        public async Task<List<TEntityType>> ListUserObjectsAsync<TEntityType>(int userId, string relation, CancellationToken cancellationToken)
            where TEntityType : Entity
        {
            _logger.TraceMethodEntry();

            var entities = await ListObjectsAsync<TEntityType, User>(userId, relation, cancellationToken).ConfigureAwait(false);

            return entities;
        }

        public async Task AddRelationshipAsync<TObjectType, TSubjectType>(int objectId, string relation, int subjectId, string? subjectRelation, CancellationToken cancellationToken = default)
            where TObjectType : Entity
            where TSubjectType : Entity
        {
            _logger.TraceMethodEntry();

            var tuples = new List<ClientTupleKey>()
            {
                new ClientTupleKey
                {
                    Object = ToZanzibarNotation<TObjectType>(objectId),
                    Relation = relation,
                    User = ToZanzibarNotation<TSubjectType>(subjectId, subjectRelation)
                }
            };

            await _openFgaClient.WriteTuples(tuples, null, cancellationToken).ConfigureAwait(false);
        }


        public async Task DeleteRelationshipAsync<TObjectType, TSubjectType>(int objectId, string relation, int subjectId, string? subjectRelation, CancellationToken cancellationToken = default)
            where TObjectType : Entity
            where TSubjectType : Entity
        {
            _logger.TraceMethodEntry();

            var tuples = new List<ClientTupleKeyWithoutCondition>()
            {
                new ClientTupleKeyWithoutCondition
                {
                    Object = ToZanzibarNotation<TObjectType>(objectId),
                    Relation = relation,
                    User = ToZanzibarNotation<TSubjectType>(subjectId, subjectRelation)
                }
            };

            await _openFgaClient.DeleteTuples(tuples, null, cancellationToken).ConfigureAwait(false);
        }

        public async Task<List<RelationTuple>> ReadAllRelationships(string? @object, string? relation, string? subject, CancellationToken cancellationToken = default)
        {
            _logger.TraceMethodEntry();

            var body = new ClientReadRequest
            {
                Object = @object,
                Relation = relation,
                User = subject,
            };

            var readResult = new List<RelationTuple>();

            string? continuationToken = null;

            do
            {
                var options = new ClientReadOptions
                {
                    PageSize = 100,
                    ContinuationToken = continuationToken
                };

                var response = await _openFgaClient
                    .Read(body, options, cancellationToken)
                    .ConfigureAwait(false);

                if (response == null)
                {
                    throw new InvalidOperationException("No Response received");
                }

                if (response.Tuples != null)
                {
                    foreach (var tuple in response.Tuples)
                    {
                        var relationTuple = new RelationTuple
                        {
                            Object = tuple.Key?.Object ?? string.Empty,
                            Relation = tuple.Key?.Relation ?? string.Empty,
                            Subject = tuple.Key?.User ?? string.Empty,
                        };

                        readResult.Add(relationTuple);
                    }
                }

                // Set the new Continuation Token to get more data ...
                continuationToken = response.ContinuationToken;

            } while (!string.IsNullOrWhiteSpace(continuationToken));

            return readResult;
        }

        public async Task<List<RelationTuple>> ReadAllRelationships<TObjectType, TSubjectType>(int? objectId, string? relation, int? subjectId, string? subjectRelation, CancellationToken cancellationToken = default)
            where TObjectType : Entity
            where TSubjectType : Entity
        {
            _logger.TraceMethodEntry();

            var body = new ClientReadRequest
            {
                Object = ToZanzibarNotation<TObjectType>(objectId),
                Relation = relation,
                User = ToZanzibarNotation<TSubjectType>(subjectId, subjectRelation),
            };

            var readResult = new List<RelationTuple>();

            string? continuationToken = null;

            do
            {
                var options = new ClientReadOptions
                {
                    PageSize = 100,
                    ContinuationToken = continuationToken
                };

                var response = await _openFgaClient
                    .Read(body, options, cancellationToken)
                    .ConfigureAwait(false);

                if (response == null)
                {
                    throw new InvalidOperationException("No Response received");
                }

                if (response.Tuples != null)
                {
                    foreach (var tuple in response.Tuples)
                    {
                        var relationTuple = new RelationTuple
                        {
                            Object = tuple.Key?.Object ?? string.Empty,
                            Relation = tuple.Key?.Relation ?? string.Empty,
                            Subject = tuple.Key?.User ?? string.Empty,
                        };

                        readResult.Add(relationTuple);
                    }
                }

                // Set the new Continuation Token to get more data ...
                continuationToken = response.ContinuationToken;

            } while (continuationToken != null);

            return readResult;
        }

        public RelationTuple GetRelationshipTuple<TObjectType, TSubjectType>(int objectId, string relation, int subjectId, string? subjectRelation)
            where TObjectType : Entity
            where TSubjectType : Entity
        {
            _logger.TraceMethodEntry();

            return new RelationTuple
            {
                Object = ToZanzibarNotation<TObjectType>(objectId),
                Relation = relation,
                Subject = ToZanzibarNotation<TSubjectType>(subjectId, subjectRelation)
            };
        }

        public async Task AddRelationshipsAsync(ICollection<RelationTuple> relationTuples, CancellationToken cancellationToken)
        {
            var clientTupleKeys = relationTuples
                .Select(x => new ClientTupleKey
                {
                    Object = x.Object,
                    Relation = x.Relation,
                    User = x.Subject
                })
                .ToList();

            await _openFgaClient
                .WriteTuples(clientTupleKeys, null, cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task DeleteRelationshipsAsync(ICollection<RelationTuple> relationTuples, CancellationToken cancellationToken)
        {
            var clientTupleKeys = relationTuples
                .Select(x => new ClientTupleKeyWithoutCondition
                {
                    Object = x.Object,
                    Relation = x.Relation,
                    User = x.Subject
                })
                .ToList();

            await _openFgaClient
                .DeleteTuples(clientTupleKeys, null, cancellationToken)
                .ConfigureAwait(false);
        }

        private static string ToZanzibarNotation<TEntity>(int? id, string? relation = null)
            where TEntity : Entity
        {
            return ToZanzibarNotation(typeof(TEntity).Name, id, relation);
        }

        private static string ToZanzibarNotation(string type, int? id, string? relation = null)
        {
            var strId = id == null ? "" : id.ToString();

            if (string.IsNullOrWhiteSpace(relation))
            {
                return $"{type}:{strId}";
            }

            return $"{type}:{strId}#{relation}";
        }

        private static (string Type, int Id, string? relation) FromZanzibarNotation(string s)
        {
            if (s.Contains('#'))
            {
                return FromZanzibarNotationWithRelation(s);
            }

            return FromZanzibarNotationWithoutRelation(s);
        }

        private static (string Type, int Id, string? relation) FromZanzibarNotationWithoutRelation(string s)
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

        private static (string Type, int Id, string? relation) FromZanzibarNotationWithRelation(string s)
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