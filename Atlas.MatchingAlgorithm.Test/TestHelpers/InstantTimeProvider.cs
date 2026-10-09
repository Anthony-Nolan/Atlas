using System;
using System.Threading;
using System.Threading.Tasks;

namespace Atlas.MatchingAlgorithm.Test.TestHelpers;

/// <summary>
/// A clock for code that waits in a loop: each timer fires at once, on the thread pool, and moves the clock on by its due
/// time. A wait of an hour takes no real time, and the code then reads the time that the wait would have reached.
/// </summary>
/// <remarks>
/// For code that waits on one timer at a time, such as a poll loop. A test runs the whole loop in order, and needs no
/// thread to move the clock.
/// </remarks>
internal sealed class InstantTimeProvider(DateTimeOffset startTime) : TimeProvider
{
    private DateTimeOffset now = startTime;

    public override DateTimeOffset GetUtcNow() => now;

    public override ITimer CreateTimer(TimerCallback callback, object state, TimeSpan dueTime, TimeSpan period)
    {
        now += dueTime;
        ThreadPool.QueueUserWorkItem(_ => callback(state));
        return new FiredTimer();
    }

    private sealed class FiredTimer : ITimer
    {
        public bool Change(TimeSpan dueTime, TimeSpan period) => false;

        public void Dispose()
        {
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
