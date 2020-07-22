using System;
using System.Threading;
using System.Threading.Tasks;
using Atlas.Common.Utils.Disposable;

namespace Atlas.Common.Utils.Concurrency
{
    public static class SemaphoreExtensions
    {
        /// <summary>
        /// Allows usage of a Semaphore in a "using" block, which will release the Semaphore on dispose.
        ///
        /// Example usage:
        ///
        /// using(await mySemaphore.SemaphoreSlot()) {
        ///     doThings();
        /// } 
        ///
        /// </summary>
        /// <param name="semaphore"></param>
        /// <returns>A <see cref="DisposableAction"/>, which will Release the Semaphore on Dispose.</returns>
        public static async Task<DisposableAction> SemaphoreSlot(this SemaphoreSlim semaphore)
        {
            await semaphore.WaitAsync();
            return new DisposableAction(() => semaphore.Release());
        }

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