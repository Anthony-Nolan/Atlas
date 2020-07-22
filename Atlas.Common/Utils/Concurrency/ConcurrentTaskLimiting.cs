using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Common.Utils.Concurrency
{
    public static class ConcurrentTaskLimiting
    {
        /// <summary>
        /// Applies the operation on every item in the collection, and wraps <see cref="WhenAll{T,TResult}"/>
        /// to limit the number of concurrent operations.
        /// </summary>
        public static async Task<IEnumerable<TResult>> WhenAll<T, TResult>(
            this IEnumerable<T> collection,
            Func<T, Task<TResult>> operation,
            int maxConcurrentOperations
        )
        {
            var semaphore = new SemaphoreSlim(maxConcurrentOperations, maxConcurrentOperations);
            return await Task.WhenAll(collection.Select(x => semaphore.WaitAndRunAsync(() => operation(x))));
        }

        /// <summary>
        /// Applies the operation on every collection in the list, and wraps <see cref="WhenAll{T,TResult}"/>
        /// to limit the number of concurrent operations.
        /// </summary>
        public static async Task WhenAll<T>(
            this IEnumerable<T> collection,
            Func<T, Task> operation,
            int maxConcurrentOperations)
        {
            var semaphore = new SemaphoreSlim(maxConcurrentOperations, maxConcurrentOperations);
            await Task.WhenAll(collection.Select(x => semaphore.WaitAndRunAsync(() => operation(x))));
        }
    }
}