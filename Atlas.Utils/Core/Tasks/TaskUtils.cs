using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Atlas.Utils.Core.Concurrency;

namespace Atlas.Utils.Core.Tasks
{
    public static class TaskUtils
    {
        private static readonly TaskFactory TaskFactory = new TaskFactory(
            CancellationToken.None,
            TaskCreationOptions.None,
            TaskContinuationOptions.None,
            TaskScheduler.Default);

        /// <summary>
        ///     Run an async function in a blocking manner. This is borrowed from the way Entity Framework runs sync methods
        /// </summary>
        /// <param name="func">The function to run.</param>
        public static void RunSync(Func<Task> func)
        {
            TaskFactory
                .StartNew(func)
                .Unwrap()
                .GetAwaiter()
                .GetResult();
        }

        public static T RunSync<T>(Func<Task<T>> func)
        {
            return TaskFactory
                .StartNew(func)
                .Unwrap()
                .GetAwaiter()
                .GetResult();
        }

        public static void RunSync(this Task task)
        {
            RunSync(() => task);
        }

        public static T RunSync<T>(this Task<T> task)
        {
            return RunSync(() => task);
        }

        /// <summary>
        /// Applies the operation on every item in the list, and wraps System.Threading.Tasks.Task.WhenAll
        /// to limit the number of concurrent operations.
        /// </summary>
        public static async Task<IEnumerable<TResult>> WhenAll<T, TResult>(
            IEnumerable<T> list, Func<T, Task<TResult>> operation, int maxConcurrentOperations)
        {
            var semaphore = new SemaphoreSlim(maxConcurrentOperations, maxConcurrentOperations);
            return await Task.WhenAll(list.Select(x => semaphore.WaitAndRunAsync(() => operation(x))));
        }

        /// <summary>
        /// Applies the operation on every item in the list, and wraps System.Threading.Tasks.Task.WhenAll
        /// to limit the number of concurrent operations.
        /// </summary>
        public static async Task WhenAll<T>(
            IEnumerable<T> list, Func<T, Task> operation, int maxConcurrentOperations)
        {
            var semaphore = new SemaphoreSlim(maxConcurrentOperations, maxConcurrentOperations);
            await Task.WhenAll(list.Select(x => semaphore.WaitAndRunAsync(() => operation(x))));
        }
    }
}