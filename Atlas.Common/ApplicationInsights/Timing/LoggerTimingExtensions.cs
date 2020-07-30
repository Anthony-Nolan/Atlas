using System;
using System.Threading.Tasks;
using LoggingStopwatch;

namespace Atlas.Common.ApplicationInsights.Timing
{
    public static class LoggerTimingExtensions
    {
        /// <summary>
        /// Tracks how long it takes to run the contents of the enclosed using block, then logs the elapsed time along with a provided message. 
        /// </summary>
        public static IDisposable RunTimed(
            this ILogger logger,
            string completionMessage,
            LogLevel logLevel = LogLevel.Info,
            bool logAtStart = false)
        {
            return new LoggingStopwatch.LoggingStopwatch(completionMessage, text => logger.SendTrace(text, logLevel), logAtStart);
        }

        public static T RunTimed<T>(
            this ILogger logger,
            string completionMessage,
            Func<T> action,
            LogLevel logLevel = LogLevel.Info,
            bool logAtStart = false
        )
        {
            using (logger.RunTimed(completionMessage, logLevel, logAtStart))
            {
                return action();
            }
        }
        public static async Task<T> RunTimedAsync<T>(
            this ILogger logger,
            string completionMessage,
            Func<Task<T>> action,
            LogLevel logLevel = LogLevel.Info,
            bool logAtStart = false
        )
        {
            using (logger.RunTimed(completionMessage, logLevel, logAtStart))
            {
                return await action();
            }
        }

        /// <summary>
        /// Allow you to select using sub-blocks to time.
        /// Tracks how long it takes to run the contents of those blocks, then logs the elapsed time along with a provided message. 
        /// </summary>
        public static LongOperationLoggingStopwatch RunLongOperationWithTimer(
            this ILogger logger,
            string completionMessage,
            LongLoggingSettings settings,
            LogLevel logLevel = LogLevel.Info)
        {
            return new LoggingStopwatch.LongOperationLoggingStopwatch(completionMessage, text => logger.SendTrace(text, logLevel), settings);
        }
    }
}