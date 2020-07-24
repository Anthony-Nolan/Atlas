/* ******************************
   **  Copyright Softwire 2020 ** 
   ****************************** */
// This was taken from a Softwire shareable Repo. At soem point it may get nugetified, in which case we might want
// migrate to that. Worth checking whether we've diverged, from the original code, though.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace LoggingStopwatch
{
    public interface ILongOperationLoggingStopwatch : IDisposable
    {
        IDisposable TimeInnerOperation();
    }

    /// <summary>
    /// Similar to <see cref="LoggingStopwatch"/>, but designed for use in situations
    /// where you want to know how long is spent in a particular block of code, over
    /// the course of multiple iterations, including when those iterations are run in parallel.
    /// 
    /// Use a single instance of the Stopwatch around the whole execution, and then
    /// repeated instances of TimeInnerOperation around the section(s?) that you want to
    /// be included in the timing.
    ///
    /// As each inner block is disposed, you'll (optionally) get a log of the number of reps
    /// completed, the progress towards completion and a prediction of completion time)
    ///
    /// When the outerStopwatch is disposed, it will log a summary of the overall time
    /// taken, and (optionally) the number of distinct threads, and a breakdown of the
    /// time spent on each.
    ///
    /// Usage:
    /// <code>
    ///     using(var stopwatch = new LongOperationLoggingStopwatch("SomeMethod", logger))
    ///     {
    ///         foreach(var thing in myThings)
    ///         {
    ///            BoringSetup();
    ///            using(stopwatch.TimeInnerOperation())
    ///            {
    ///                InterestingMethod(thing);
    ///            }
    ///            MoreBoringWork()
    ///         }
    ///     }//Log will be written here.
    /// </code>
    /// 
    /// Also accepts anonymous lambda as a logger: <c>new LongOperationLoggingStopwatch("AnotherMethod", (text) => myLogger.WriteLog(text))</c>
    /// </summary>
    public class LongOperationLoggingStopwatch : LoggingStopwatch, ILongOperationLoggingStopwatch
    {
        private readonly LongLoggingSettings settings;
        private int iterationsCompleted = 0;
        private int activeExecutions = 0;
        private readonly InnerOperationExecutionTimer innerTimingHandler;

        #region Constructors
        /// <inheritdoc cref="LongOperationLoggingStopwatch"/>
        /// <param name="identifier">
        /// String to identify in the logs what operation was timed.
        /// A unique identifier will be generated in addition to this, so that multiple executions are distinguishable.
        /// </param>
        /// <param name="loggingAction">Method to call whenever some text should be logged.</param>
        /// <param name="loggingSettings">
        /// Override the defaults for how the inner operations get logged.
        /// </param>
        public LongOperationLoggingStopwatch(string identifier, Action<string> loggingAction, LongLoggingSettings loggingSettings = null) :
            base(identifier, loggingAction)
        {
            settings?.Validate();
            settings = loggingSettings ?? new LongLoggingSettings();
            LogInitiationMessage();
            innerTimingHandler = new InnerOperationExecutionTimer(this);
            base.timer.Start(); // Zero out the time taken in this ctor, since the end of the base ctor.
        }

        private void LogInitiationMessage()
        {
            var initiationMessage = "Started.";
            var reps = settings.ExpectedNumberOfIterations;
            if (reps.HasValue)
            {
                initiationMessage += $"|Expecting to complete {reps} iterations of the inner operation.";
            }

            if (settings.ReportOuterTimerStart)
            {
                Log(initiationMessage);
            }
        }
        #endregion

        /// <summary>
        /// Defines the block to be timed.
        /// The overhead that calling this introduces on a single thread is around 0.15 microseconds.
        /// Overhead on multiple threads is hard to judge, but it certainly seems to be within an order of magnitude.
        /// For comparison, DateTime.Now takes ~0.3 microseconds and DateTime.UtcNow takes ~0.07 microseconds
        ///
        /// Note that if the logging frequency is low, then this can get much more expensive, as building
        /// the logging string is non-trivial.
        ///
        /// If logging is enabled on EVERY operation (even with a no-op logger!), then the overhead jumps to
        /// about 2 microseconds!
        /// </summary>
        /// <returns></returns>
        public IDisposable TimeInnerOperation()
        {
            Interlocked.Increment(ref activeExecutions);
            innerTimingHandler.StartOperation();
            return innerTimingHandler;
        }

        // Needs to be fully thread-safe!
        private void ReportInnerExecutionComplete()
        {
            var newCompletedCount = Interlocked.Increment(ref iterationsCompleted);
            LogPerExecutionMessageIfAppropriate(settings, newCompletedCount, base.timer, base.Log);
            Interlocked.Decrement(ref activeExecutions);
        }

        public override void Dispose()
        {
            LogFinalTimingReport();
        }

        // This is deliberately static, so that we're forced to explicitly pass in captured
        // values and can't use instance fields that might have been updated by other threads.
        // This is to ensure that it is fully threadsafe.
        private static void LogPerExecutionMessageIfAppropriate(LongLoggingSettings settings, int newCompletedCount, Stopwatch outerStopwatch, Action<string> logAction)
        {
            if (newCompletedCount % settings.InnerOperationLoggingPeriod == 0)
            {
                try
                {
                    var elapsedOuterTime_MS = outerStopwatch.ElapsedMilliseconds; //Dont pass in just the ElapsedMillis, since that's not completely trivial for it to calculate.
                    var logMessage = $"Progress: ({newCompletedCount}) operations completed.";

                    if (settings.ExpectedNumberOfIterations.HasValue)
                    {
                        var completionPercentage = Decimal.Divide(newCompletedCount, settings.ExpectedNumberOfIterations.Value);

                        if (settings.ReportPercentageCompletion)
                        {
                            logMessage += $"|{completionPercentage:0.00%}";
                        }

                        if (settings.ReportProjectedCompletionTime)
                        {
                            var remainingPercentage = 1 - completionPercentage;
                            var remainingMultiplier = remainingPercentage / completionPercentage;

                            var projectedTotalTimeRemaining_MS = elapsedOuterTime_MS * remainingMultiplier;
                            var projectedOuterCompletionTime = DateTime.UtcNow.AddMilliseconds((double) projectedTotalTimeRemaining_MS);
                            logMessage += $"|Projected completion time: {projectedOuterCompletionTime}Z (UTC)";
                        }
                    }

                    logAction(logMessage);
                }
                catch (Exception e)
                {
                    // We really don't expect exceptions above, but if anything goes
                    // wrong we don't want it to bring down the calling operation.
                    logAction("Swallowing exception in LoggingStopwatch: " + e.ToString());
                }
            }

        }

        public void LogFinalTimingReport()
        {
            var overallTime = base.timer.Elapsed;
            //TODO: What about errors?

            if (activeExecutions > 0)
            {
                Log("WARNING: Some inner executions were still outstanding when the outer stopwatch was Disposed! Reporting will ignore those executions.");
            }

            if (iterationsCompleted == 0)
            {
                Log($"Completed in {overallTime}");
                return;
            }

            var threadCountMessage = $"Inner operations were spread over {innerTimingHandler.DistinctThreads} thread(s):";

            //Log the high-level results.
            var primaryLogMessage = $"Completed|{iterationsCompleted} Inner operations ran for a linear total of: {innerTimingHandler.TotalLinearTime}|The outer scope ran for an elapsed time of: {overallTime}";
            if (settings.ReportThreadCount && !settings.ReportPerThreadTime)
            {
                primaryLogMessage += $"|{threadCountMessage}";
            }
            
            Log(primaryLogMessage);
            
            //Log per-thread results if requested.
            if (settings.ReportPerThreadTime)
            {
                var innerThreadTimes = innerTimingHandler.ListAllThreadTimes();

                if (innerThreadTimes.Count == 1)
                {
                    Log("All inner operations ran on a single thread.");
                }
                else
                {
                    Log(threadCountMessage);
                    for (int i = 0; i < innerThreadTimes.Count; i++)
                    {
                        var threadTimeSpan = innerThreadTimes[i];
                        Log($" - Time spent on thread #{i}: {threadTimeSpan}");
                    }
                }
            }
        }

        /// <summary>
        /// We have a single copy of this for each outer loop, which gets `StartOperation` called on it repeatedly for each inner loop. 
        /// </summary>
        private class InnerOperationExecutionTimer : IDisposable
        {
            private readonly LongOperationLoggingStopwatch parent;

            // The StartOperation() method is going to be called from multiple threads, in parallel, and we want each call to Start a different StopWatch.
            // That's exactly what ThreadLocal does - gives you distinct objects on distinct threads.
            // Unfortunately, ThreadLocal will NOT (necessarily) give you the same object when called either side of an await call! https://stackoverflow.com/questions/48973599/c-net-using-threadlocal-with-async-await
            // Which would mean that the Stop() method wouldn't necessarily get the same timer that we started previously.
            // Fortunately, AsyncLocal DOES guarantee to give you the same object on either side of the await call.
            // But AsyncLocal doesn't keep track of all the OTHER threads :(
            // So we need to use ThreadLocal in the StartOperation to *provision* the StopWatch, and AsyncLocal to retain a reference to it on the other side of the .Dispose call.

            private readonly ThreadLocal<Stopwatch> timers_OnThread = new ThreadLocal<Stopwatch>(() => new Stopwatch(), true);
            private readonly AsyncLocal<Stopwatch> asyncSafeTimerHolder = new AsyncLocal<Stopwatch>();

            public InnerOperationExecutionTimer(LongOperationLoggingStopwatch parent)
            {
                this.parent = parent;
            }

            public void StartOperation()
            {
                var thisExecutionTimer = timers_OnThread.Value;
                asyncSafeTimerHolder.Value = thisExecutionTimer;
                thisExecutionTimer.Start();
            }

            public void Dispose()
            {
                var thisExecutionTimer = asyncSafeTimerHolder.Value;
                thisExecutionTimer.Stop();
                parent.ReportInnerExecutionComplete();
            }

            public IList<TimeSpan> ListAllThreadTimes() => timers_OnThread.Values.Select(watch => watch.Elapsed).Where(elapsed => elapsed != TimeSpan.Zero).ToArray();
            public int DistinctThreads => ListAllThreadTimes().Count;
            public TimeSpan TotalLinearTime => new TimeSpan(ListAllThreadTimes().Sum(watches => watches.Ticks));
        }
    }
}
