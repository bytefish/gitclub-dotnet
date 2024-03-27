﻿// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using OpenFga.Sdk.Client.Model;
using OpenFga.Sdk.Client;
using GitClub.Database;
using GitClub.Database.Models;
using Microsoft.EntityFrameworkCore;
using GitClub.Models;
using GitClub.Infrastructure.Logging;
using GitClub.Infrastructure.OpenFga;
using OpenFga.Sdk.Model;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

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
                Object = ZanzibarFormatters.ToZanzibarNotation<TObjectType>(objectId),
                User = ZanzibarFormatters.ToZanzibarNotation<TSubjectType>(subjectId),
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

        public async Task<bool> CheckObjectAsync<TObjectType, TSubjectType>(TObjectType @object, string relation, TSubjectType @subject, CancellationToken cancellationToken)
            where TSubjectType : Entity
            where TObjectType : Entity
        {
            _logger.TraceMethodEntry();

            var allowed = await CheckObjectAsync<TObjectType, TSubjectType>(@object.Id, relation, subject.Id, cancellationToken).ConfigureAwait(false);

            return allowed;
        }

        public async Task<bool> CheckObjectAsync<TObjectType, TSubjectType>(TObjectType @object, string relation, int subjectId, CancellationToken cancellationToken)
            where TSubjectType : Entity
            where TObjectType : Entity
        {
            _logger.TraceMethodEntry();

            var allowed = await CheckObjectAsync<TObjectType, TSubjectType>(@object.Id, relation, subjectId, cancellationToken).ConfigureAwait(false);

            return allowed;
        }

        public async Task<List<(bool Allowed, TObjectType Object)>> CheckObjectsParallelAsync<TObjectType, TSubjectType>(List<TObjectType> objects, string relation, int subjectId, CancellationToken cancellationToken)
            where TSubjectType : Entity
            where TObjectType : Entity
        {
            _logger.TraceMethodEntry();

            var results = new ConcurrentBag<(bool Allowed, int Index, TObjectType Object)>();

            var objectsWithIndex = objects
                .Select((o, i) => (Index: i, Object: o))
                .ToList();

            await Parallel.ForEachAsync(objectsWithIndex, async (o, ct) =>
            {
                bool isAuthorized = await this
                    .CheckObjectAsync<TObjectType, TSubjectType>(o.Object, relation, subjectId, cancellationToken)
                    .ConfigureAwait(false);

                var result = (Allowed: isAuthorized, Index: o.Index, Object: o.Object);

                results.Add(result);
            });

            var unorderedResults = results.ToList();

            return unorderedResults
                .OrderBy(x => x.Index)
                .Select(x => (Allowed: x.Allowed, Object: x.Object))
                .ToList();
        }

        public async Task<List<(bool Allowed, TObjectType Object)>> CheckObjectsParallelAsync<TObjectType, TSubjectType>(List<TObjectType> objects, string relation, TSubjectType subject, CancellationToken cancellationToken)
            where TSubjectType : Entity
            where TObjectType : Entity
        {
            _logger.TraceMethodEntry();

            var allowed = await CheckObjectsParallelAsync<TObjectType, TSubjectType>(objects, relation, subject.Id, cancellationToken).ConfigureAwait(false);

            return allowed;
        }

        public async Task<bool> CheckUserObjectAsync<TObjectType>(int userId, int objectId, string relation, CancellationToken cancellationToken)
            where TObjectType : Entity
        {
            _logger.TraceMethodEntry();

            var allowed = await CheckObjectAsync<TObjectType, User>(objectId, relation, userId, cancellationToken).ConfigureAwait(false);

            return allowed;
        }

        public async Task<bool> CheckUserObjectAsync<TObjectType>(int userId, TObjectType @object, string relation, CancellationToken cancellationToken)
            where TObjectType : Entity
        {
            _logger.TraceMethodEntry();

            var allowed = await CheckObjectAsync<TObjectType, User>(@object.Id, relation, userId, cancellationToken).ConfigureAwait(false);

            return allowed;
        }

        public async Task<List<(bool Allowed, TObjectType Object)>> CheckUserObjectsParallelAsync<TObjectType>(int userId, List<TObjectType> objects, string relation, CancellationToken cancellationToken)
            where TObjectType : Entity
        {
            _logger.TraceMethodEntry();

            var allowed = await CheckObjectsParallelAsync<TObjectType, User>(objects, relation, userId, cancellationToken).ConfigureAwait(false);

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
                User = ZanzibarFormatters.ToZanzibarNotation<TSubjectType>(subjectId),
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
                .Select(x => ZanzibarFormatters.FromZanzibarNotation(x))
                .Select(x => x.Id)
                .ToArray();

            var entities = await _applicationDbContext.Set<TObjectType>().AsNoTracking()
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
                    Object = ZanzibarFormatters.ToZanzibarNotation<TObjectType>(objectId),
                    Relation = relation,
                    User = ZanzibarFormatters.ToZanzibarNotation<TSubjectType>(subjectId, subjectRelation)
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
                    Object = ZanzibarFormatters.ToZanzibarNotation<TObjectType>(objectId),
                    Relation = relation,
                    User = ZanzibarFormatters.ToZanzibarNotation<TSubjectType>(subjectId, subjectRelation)
                }
            };

            await _openFgaClient.DeleteTuples(tuples, null, cancellationToken).ConfigureAwait(false);
        }

        public async IAsyncEnumerable<RelationTuple> ReadTuplesAsync(string? @object, string? relation, string? subject, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            var body = new ClientReadRequest
            {
                Object = @object ?? string.Empty,
                Relation = relation ?? string.Empty,
                User = subject ?? string.Empty
            };

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

                        yield return relationTuple;
                    }
                }

                // Set the new Continuation Token to get more data ...
                continuationToken = response.ContinuationToken;

            } while (!string.IsNullOrWhiteSpace(continuationToken));
        }

        public IAsyncEnumerable<RelationTuple> ReadTuplesAsync<TObjectType, TSubjectType>(int? objectId, string? relation, int? subjectId, string? subjectRelation, CancellationToken cancellationToken = default)
            where TObjectType : Entity
            where TSubjectType : Entity
        {
            _logger.TraceMethodEntry();

            return ReadTuplesAsync(
                @object: ZanzibarFormatters.ToZanzibarNotation<TObjectType>(objectId),
                relation: relation,
                subject: ZanzibarFormatters.ToZanzibarNotation<TSubjectType>(subjectId, subjectRelation), cancellationToken);

        }

        public async Task AddRelationshipsAsync(ICollection<RelationTuple> relationTuples, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

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
            _logger.TraceMethodEntry();

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

        public async Task WriteAsync(ICollection<RelationTuple> writes, ICollection<RelationTuple> deletes, CancellationToken cancellationToken)
        {
            _logger.TraceMethodEntry();

            var body = new ClientWriteRequest()
            {
                Writes = writes
                    .Select(x => ToClientTupleKey(x))
                    .ToList(),
                Deletes = deletes
                    .Select(x => ToClientTupleKeyWithoutCondition(x))
                    .ToList()
            };

            await _openFgaClient
                .Write(body, null, cancellationToken)
                .ConfigureAwait(false);
        }

        private ClientTupleKey ToClientTupleKey(RelationTuple source)
        {
            return new ClientTupleKey
            {
                Object = source.Object,
                Relation = source.Relation,
                User = source.Subject
            };
        }

        private ClientTupleKeyWithoutCondition ToClientTupleKeyWithoutCondition(RelationTuple source)
        {
            return new ClientTupleKeyWithoutCondition
            {
                Object = source.Object,
                Relation = source.Relation,
                User = source.Subject
            };
        }

    }
}