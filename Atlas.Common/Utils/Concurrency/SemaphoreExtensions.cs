using System;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.Common.Utils.Concurrency
{
    public static class SemaphoreExtensions
    {
        /// <summary>
        /// Waits until a slot in the Semaphore is available, acquires the slot, applies the operation, then frees the slot.
        /// </summary>
        public static async Task WaitAndRunAsync(this SemaphoreSlim semaphore, Func<Task> operation)
        {
            try
            {
                await semaphore.WaitAsync();
                await operation();
            }
            finally
            {
                semaphore.Release();
            }
        }

        /// <summary>
        /// Waits until a slot in the Semaphore is available, acquires the slot, applies the operation, then frees the slot.
        /// </summary>
        public static async Task<T> WaitAndRunAsync<T>(this SemaphoreSlim semaphore, Func<Task<T>> operation)
        {
            try
            {
                await semaphore.WaitAsync();
                return await operation();
            }
            finally
            {
                semaphore.Release();
            }
        }
    }
}