using System;
using System.Threading;
using System.Threading.Tasks;

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
    }
}
