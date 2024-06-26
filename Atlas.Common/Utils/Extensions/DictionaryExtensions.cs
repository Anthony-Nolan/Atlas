using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Atlas.Common.Utils.Extensions
{
    public static class DictionaryExtensions
    {
        public static void AddIfNotNullOrEmpty<TKey>(this IDictionary<TKey, string> dictionary, TKey key, string value)
        {
            if (!value.IsNullOrEmpty())
            {
                dictionary.Add(key, value);
            }
        }

        public static void AddIfNonNull<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
        {
            if (value != null)
            {
                dictionary.Add(key, value);
            }
        }

        public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
        {
            return dictionary.TryGetValue(key, out var result) ? result : default;
        }

        /// <summary>
        /// If the dictionary contains the specified key, returns the corresponding value.
        /// Otherwise, generates the value with the provided factory, sets it in the dictionary, and returns the generated value.
        /// </summary>
        public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TValue> valueFactory)
        {
            if (!dictionary.TryGetValue(key, out var value))
            {
                value = valueFactory();
                dictionary.Add(key, value);
            }

            return value;
        }

        /// <summary>
        /// Async version of <see cref="GetOrAdd{TKey,TValue}"/>.
        /// </summary>
        public static async Task<TValue> GetOrAddAsync<TKey, TValue>(
            this IDictionary<TKey, TValue> dictionary,
            TKey key,
            Func<Task<TValue>> valueFactoryAsync)
        {
            if (!dictionary.TryGetValue(key, out var value))
            {
                value = await valueFactoryAsync();
                dictionary.Add(key, value);
            }

            return value;
        }

        /// <summary>
        /// Merges two dictionaries into one.
        /// Any unique values will be preserved.
        /// Any duplicate key/value pairs will be de-duplicated and returned once.
        /// Any duplicate keys with differing values make the merge invalid - <see cref="ArgumentException"/> will be thrown.
        /// </summary>
        public static Dictionary<TKey, TValue> Merge<TKey, TValue>(
            this IReadOnlyDictionary<TKey, TValue> dictionary,
            IReadOnlyDictionary<TKey, TValue> otherDictionary)
        {
            return dictionary.ToList()
                .Concat(otherDictionary.ToList())
                .ToHashSet()
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }
    }
}