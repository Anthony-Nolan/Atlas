using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Atlas.Common.Utils.Extensions
{
    public static class EnumerableExtensions
    {
        public static bool IsNullOrEmpty(this IEnumerable source)
        {
            return source == null || source.GetEnumerator().MoveNext() == false;
        }

        public static async Task<Dictionary<TKey, TValue>> ToDictionaryAsync<TInput, TKey, TValue>(
            this IEnumerable<TInput> enumerable,
            Func<TInput, TKey> syncKeySelector,
            Func<TInput, Task<TValue>> asyncValueSelector)
        {
            var dictionary = new Dictionary<TKey, TValue>();

            foreach (var item in enumerable)
            {
                var key = syncKeySelector(item);

                var value = await asyncValueSelector(item);

                dictionary.Add(key, value);
            }

            return dictionary;
        }

        public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(
            this IEnumerable<KeyValuePair<TKey, TValue>> keyValuePair)
        {
            return keyValuePair.ToDictionary(p => p.Key, p => p.Value);
        }

        /// <summary>
        /// Splits a collection into 2 groups whilst performing only a single pass through the data.
        /// </summary>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="values">Objects to be split</param>
        /// <param name="splittingFunction">Condition upon which to split the objects</param>
        /// <returns>
        /// A ValueTuple containing:
        /// FIRST, the elements that DO satisfy the condition, and
        /// SECOND, the elements that do NOT satisfy the condition.</returns>
        public static (List<TValue>, List<TValue>) ReifyAndSplit<TValue>(
            this IEnumerable<TValue> values,
            Func<TValue, bool> splittingFunction)
        {
            var lookup = values.ToLookup(splittingFunction);
            return (lookup[true].ToList(), lookup[false].ToList());
        }
    }
}