using System;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.MatchPrediction.Services.MatchProbability
{
    /// <summary>
    /// Process-wide limiter bounding the TOTAL number of donors being match-predicted concurrently, across every
    /// batch message in flight.
    ///
    /// The ACA worker uses a <c>ServiceBusProcessor</c> that processes up to <c>MaxConcurrentCalls</c> batch messages
    /// at once. Without a shared budget, each batch's <see cref="ParallelMatchPredictionAlgorithm"/> would create its
    /// own <c>SemaphoreSlim(MaxParallelism)</c>, so the effective donor concurrency would be
    /// <c>MaxConcurrentCalls × MaxParallelism</c> — multiplying CPU and (critically) peak memory, since each donor's
    /// genotype expansion allocates heavily. It would also leave a draining batch's freed slots idle while other
    /// batches queue.
    ///
    /// Registering this as a singleton gives all batches a single donor budget: a slot released by a finishing donor
    /// in one batch is immediately reused by a donor from another batch, keeping the budget saturated without raising
    /// the peak.
    /// </summary>
    public interface IDonorProcessingLimiter
    {
        /// <summary>Acquires a slot (waiting if necessary), runs the operation, then releases the slot.</summary>
        Task<T> RunAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default);

        /// <summary>Number of currently free slots — exposed for diagnostics / backpressure.</summary>
        int AvailableSlots { get; }
    }

    public sealed class DonorProcessingLimiter : IDonorProcessingLimiter, IDisposable
    {
        private readonly SemaphoreSlim semaphore;

        public DonorProcessingLimiter(int maxConcurrentDonors)
        {
            if (maxConcurrentDonors < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxConcurrentDonors), maxConcurrentDonors, "Concurrent donor limit must be at least 1.");
            }

            semaphore = new SemaphoreSlim(maxConcurrentDonors, maxConcurrentDonors);
        }

        public int AvailableSlots => semaphore.CurrentCount;

        public async Task<T> RunAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default)
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                return await operation();
            }
            finally
            {
                semaphore.Release();
            }
        }

        public void Dispose() => semaphore.Dispose();
    }
}
