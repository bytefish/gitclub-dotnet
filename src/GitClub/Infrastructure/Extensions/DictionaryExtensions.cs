using System.Diagnostics.CodeAnalysis;

namespace GitClub.Infrastructure.Outbox.Stream
{
    public static class DictionaryExtensions
    {
        /// <summary>
        /// Tries to get a value from a <see cref="IDictionary{TKey, TValue}"> and tries 
        /// to cast it to the given type <typeparamref name="T"/>. If the given key doesn't 
        /// exist, we are returning a <paramref name="defaultValue"/>.
        /// </summary>
        /// <typeparam name="T">Target Type to try cast to</typeparam>
        /// <param name="values">Source Dictionary with values</param>
        /// <param name="key">The key to get</param>
        /// <param name="defaultValue">The default value returned, when <paramref name="key"/> does not exist</param>
        /// <returns>The value as <typeparamref name="T"/></returns>
        /// <exception cref="InvalidOperationException">Throws, if the cast isn't possible</exception>
        public static T? GetOptionalValue<T>(this IDictionary<string, object?> values, string key, T? defaultValue = default(T))
        {
            if (!values.ContainsKey(key))
            {
                return defaultValue;
            }

            var untypedValue = values[key];

            if (untypedValue == null)
            {
                return defaultValue;
            }

            if (!TryCast<T>(untypedValue, out var typedValue))
            {
                throw new InvalidOperationException($"Failed to cast to '{typeof(T)}'");
            }

            return typedValue;
        }

        /// <summary>
        /// Gets a Value from a <see cref="IDictionary{TKey, TValue}"> and tries 
        /// to cast it to the given type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">Target Type to try cast to</typeparam>
        /// <param name="values">Source Dictionary with values</param>
        /// <param name="key">The key to get</param>
        /// <returns>The value as <typeparamref name="T"/></returns>
        /// <exception cref="InvalidOperationException">Throws, if the key doesn't exist or a cast isn't possible</exception>
        public static T GetRequiredValue<T>(this IDictionary<string, object?> values, string key)
        {
            if (!values.ContainsKey(key))
            {
                throw new InvalidOperationException($"Value is required for key '{key}'");
            }

            var untypedValue = values[key];

            if (untypedValue == null)
            {
                throw new InvalidOperationException($"Value is required for key '{key}'");
            }

            if (!TryCast<T>(untypedValue, out var typedValue))
            {
                throw new InvalidOperationException($"Failed to cast to '{typeof(T)}'");
            }

            return typedValue;
        }

        /// <summary>
        /// Casts to a value of the given type if possible.
        /// If <paramref name="obj"/> is <see langword="null"/> and <typeparamref name="T"/>
        /// can be <see langword="null"/>, the cast succeeds just like the C# language feature.
        /// </summary>
        /// <param name="obj">The object to cast.</param>
        /// <param name="value">The value of the object, if the cast succeeded.</param>
        internal static bool TryCast<T>(object? obj, [NotNullWhen(true)] out T? value)
        {
            if (obj is T tObj)
            {
                value = tObj;
                return true;
            }

            value = default(T);
            return obj is null && default(T) is null;
        }
    }
}
}
