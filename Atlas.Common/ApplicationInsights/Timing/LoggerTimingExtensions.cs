using System;
using System.Collections.Generic;
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
            bool logAtStart = false,
            bool verboseAtStart = false)
        {
            if (verboseAtStart)
            {
                logger.SendTrace($"Just started: {completionMessage}", LogLevel.Verbose);
            }

            return new LoggingStopwatch.LoggingStopwatch(
                completionMessage, (text, milliseconds) => logger.SendTrace(text, logLevel, BuildProperties(milliseconds)),
                logAtStart
            );
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
            return new LoggingStopwatch.LongOperationLoggingStopwatch(
                completionMessage,
                (text, milliseconds) => logger.SendTrace(text, logLevel, BuildProperties(milliseconds)),
                settings);
        }

        private static Dictionary<string, string> BuildProperties(long? milliseconds)
        {
            return milliseconds == null ? null : new Dictionary<string, string> {{"Milliseconds", milliseconds.ToString()}};
        }
    }
}