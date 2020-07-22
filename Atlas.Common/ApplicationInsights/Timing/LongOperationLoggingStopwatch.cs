/* ******************************
   **  Copyright Softwire 2020 ** 
   ****************************** */
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
        /// <param name="logger">
        /// Logger object to perform the logging when complete
        /// </param>
        /// <param name="loggingSettings">
        /// Override the defaults for how the inner operations get logged.
        /// </param>
        public LongOperationLoggingStopwatch(string identifier, IStopwatchLogger logger, LongLoggingSettings loggingSettings = null) :
            base(identifier, logger)
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

        /// <inheritdoc/>
        /// <param name="identifier">Defers to Inherited paramDoc</param>
        /// <param name="loggingAction">Method to call whenever some text should be logged.</param>
        /// <param name="settings">Defers to Inherited paramDoc</param>
        public LongOperationLoggingStopwatch(string identifier, Action<string> loggingAction, LongLoggingSettings settings = null)
            : this(identifier, new LambdaLogger(loggingAction), settings)
        {
        }

        //Note if you copy-paste this code, feel free to delete this if you don't want the Microsoft.Extensions.Logging dependency.
        /// <inheritdoc/>
        /// <param name="identifier">Defers to Inherited paramDoc</param>
        /// <param name="microsoftLogger">Accepts an <see cref="ILogger"/> and logs to it at the <see cref="LogLevel.Information"/> level.</param>
        /// <param name="logAtStart">Defers to Inherited paramDoc</param>
        public LongOperationLoggingStopwatch(string identifier, ILogger microsoftLogger, LongLoggingSettings settings = null)
            : this(identifier, new MicrosoftLoggerWrapper(microsoftLogger), settings)
        { }
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
                            var projectedOuterCompletionTime = DateTime.UtcNow.AddMilliseconds((double)projectedTotalTimeRemaining_MS);
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
            private readonly ThreadLocal<Stopwatch> timer = new ThreadLocal<Stopwatch>(() => new Stopwatch(), true);

            public InnerOperationExecutionTimer(LongOperationLoggingStopwatch parent)
            {
                this.parent = parent;
            }

            public void StartOperation()
            {
                timer.Value.Start();
            }

            public void Dispose()
            {
                timer.Value.Stop();
                parent.ReportInnerExecutionComplete();
            }

            public IList<TimeSpan> ListAllThreadTimes() => timer.Values.Select(watch => watch.Elapsed).ToArray();
            public int DistinctThreads => timer.Values.Count;
            public TimeSpan TotalLinearTime => new TimeSpan(timer.Values.Sum(watches => watches.ElapsedTicks));
        }
    }
}
