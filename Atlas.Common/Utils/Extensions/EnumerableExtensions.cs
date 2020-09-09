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

        public static IEnumerable<T> Except<T>(this IEnumerable<T> enumerable, T singleItem)
        {
            return enumerable.Except(new[] {singleItem});
        }

        /// <summary>
        /// Filters a collection based on a provided function. If an object is filtered (i.e the filterFunction returned false)
        /// then a callback function will be called, with the object as its only parameter.
        /// </summary>
        /// <param name="enumerable"> The collection to be filtered</param>
        /// <param name="filterFunction"> The filterFunction. If this returns true, the object is kept in the collection,
        ///  otherwise it is removed, and the callback function is called.</param>
        /// <param name="callback"> if an object is filtered out this callback will be called</param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static IEnumerable<T> FilterAndCallbackIfFiltered<T>(this IEnumerable<T> enumerable, Func<T, bool> filterFunction, Action<T> callback)
        {
            foreach (var obj in enumerable)
            {
                if (filterFunction(obj))
                {
                    yield return obj;
                }
                else
                {
                    callback(obj);
                }
            }
        }

        /// <summary>
        /// When summing decimals, we should always sort in increasing size first, to avoid loss of data via loss of floating point precision
        /// See https://stackoverflow.com/questions/6699066/in-which-order-should-floats-be-added-to-get-the-most-precise-result for an
        /// explanation on why this is recommended
        /// </summary>
        public static decimal SumDecimals(this IEnumerable<decimal> decimals) => decimals.OrderBy(x => x).Sum();
    }
}