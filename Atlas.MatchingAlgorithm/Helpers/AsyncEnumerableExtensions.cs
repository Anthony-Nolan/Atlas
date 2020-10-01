using System;
using System.Collections.Generic;
using System.Linq;

namespace Atlas.MatchingAlgorithm.Helpers
{
    internal static class AsyncEnumerableExtensions
    {
        // Used to avoid name clashes with base LINQ, as IAsyncEnumerator is also an IEnumerable
        // Causes further clashes with EF if this is added to the common project.
        internal static IAsyncEnumerable<T> WhereAsync<T>(this IAsyncEnumerable<T> enumerable, Func<T, bool> filter)
        {
            return enumerable.Where(filter);
        }
        
        // Used to avoid name clashes with base LINQ, as IAsyncEnumerator is also an IEnumerable
        // Causes further clashes with EF if this is added to the common project.
        internal static IAsyncEnumerable<TResult> SelectAsync<T, TResult>(this IAsyncEnumerable<T> enumerable, Func<T, TResult> map)
        {
            return enumerable.Select(map);
        }
    }
}