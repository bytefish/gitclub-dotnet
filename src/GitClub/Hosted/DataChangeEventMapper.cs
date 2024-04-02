//using GitClub.Database.Models;
//using NodaTime;
//using System.Diagnostics.CodeAnalysis;

//namespace GitClub.Hosted
//{

//    public class OrganizationMapper
//    {

//        public OrganizationMapper(ILogger logger)
//        {
//        }

//        public override string SchemaName => "gitclub";

//        public override string TableName => "organization";

//        public override ValueTask<Organization> MapResults(Relation relation, IDictionary<string, object?> values, CancellationToken cancellationToken)
//        {
//            _logger.TraceMethodEntry();

//            var organization = new Organization
//            {
//                Id = values.GetRequiredValue<int>("organization_id"),
//                Name = values.GetRequiredValue<string>("name"),
//                BaseRepositoryRole = (BaseRepositoryRoleEnum)values.GetRequiredValue<int>("base_repository_role"),
//                LastEditedBy = values.GetRequiredValue<int>("last_edited_by"),
//                SysPeriod = values.GetRequiredValue<Interval>("sys_period"),
//                BillingAddress = values.GetOptionalValue<string>("billing_address")
//            };

//            return ValueTask.FromResult(organization);
//        }
//    }

//    public class TeamMapper : ReplicationTupleMapper<Team>
//    {
//        public TeamMapper(ILogger logger) : base(logger)
//        {
//        }

//        public override string SchemaName => "gitclub";

//        public override string TableName => "team";

//        public override ValueTask<Team> MapResults(Relation relation, IDictionary<string, object?> values, CancellationToken cancellationToken)
//        {
//            _logger.TraceMethodEntry();

//            var team = new Team
//            {
//                Id = values.GetRequiredValue<int>("team_id"),
//                Name = values.GetRequiredValue<string>("name"),
//                OrganizationId = values.GetRequiredValue<int>("organization_id"),
//                LastEditedBy = values.GetRequiredValue<int>("last_edited_by"),
//                SysPeriod = values.GetRequiredValue<Interval>("sys_period")
//            };

//            return ValueTask.FromResult(team);
//        }
//    }

//    public static class DictionaryExtensions
//    {
//        /// <summary>
//        /// Tries to get a value from a <see cref="IDictionary{TKey, TValue}"> and tries 
//        /// to cast it to the given type <typeparamref name="T"/>. If the given key doesn't 
//        /// exist, we are returning a <paramref name="defaultValue"/>.
//        /// </summary>
//        /// <typeparam name="T">Target Type to try cast to</typeparam>
//        /// <param name="values">Source Dictionary with values</param>
//        /// <param name="key">The key to get</param>
//        /// <param name="defaultValue">The default value returned, when <paramref name="key"/> does not exist</param>
//        /// <returns>The value as <typeparamref name="T"/></returns>
//        /// <exception cref="InvalidOperationException">Throws, if the cast isn't possible</exception>
//        public static T? GetOptionalValue<T>(this IDictionary<string, object?> values, string key, T? defaultValue = default(T))
//        {
//            if (!values.ContainsKey(key))
//            {
//                return defaultValue;
//            }

//            var untypedValue = values[key];

//            if (untypedValue == null)
//            {
//                return defaultValue;
//            }

//            if (!TryCast<T>(untypedValue, out var typedValue))
//            {
//                throw new InvalidOperationException($"Failed to cast to '{typeof(T)}'");
//            }

//            return typedValue;
//        }

//        /// <summary>
//        /// Gets a Value from a <see cref="IDictionary{TKey, TValue}"> and tries 
//        /// to cast it to the given type <typeparamref name="T"/>.
//        /// </summary>
//        /// <typeparam name="T">Target Type to try cast to</typeparam>
//        /// <param name="values">Source Dictionary with values</param>
//        /// <param name="key">The key to get</param>
//        /// <returns>The value as <typeparamref name="T"/></returns>
//        /// <exception cref="InvalidOperationException">Throws, if the key doesn't exist or a cast isn't possible</exception>
//        public static T GetRequiredValue<T>(this IDictionary<string, object?> values, string key)
//        {
//            if (!values.ContainsKey(key))
//            {
//                throw new InvalidOperationException($"Value is required for key '{key}'");
//            }

//            var untypedValue = values[key];

//            if (untypedValue == null)
//            {
//                throw new InvalidOperationException($"Value is required for key '{key}'");
//            }

//            if (!TryCast<T>(untypedValue, out var typedValue))
//            {
//                throw new InvalidOperationException($"Failed to cast to '{typeof(T)}'");
//            }

//            return typedValue;
//        }

//        /// <summary>
//        /// Casts to a value of the given type if possible.
//        /// If <paramref name="obj"/> is <see langword="null"/> and <typeparamref name="T"/>
//        /// can be <see langword="null"/>, the cast succeeds just like the C# language feature.
//        /// </summary>
//        /// <param name="obj">The object to cast.</param>
//        /// <param name="value">The value of the object, if the cast succeeded.</param>
//        internal static bool TryCast<T>(object? obj, [NotNullWhen(true)] out T? value)
//        {
//            if (obj is T tObj)
//            {
//                value = tObj;
//                return true;
//            }

//            value = default(T);
//            return obj is null && default(T) is null;
//        }
//    }

//}
