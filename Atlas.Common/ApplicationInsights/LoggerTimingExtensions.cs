using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Atlas.Common.ApplicationInsights
{
    public static class LoggerTimingExtensions
    {
        
        /// <summary>
        /// Runs the provided action, tracking how long it takes to operate, then logs the elapsed time along with a provided message. 
        /// </summary>
        public static T RunTimed<T>(
            this ILogger logger,
            Func<T> action,
            string completionMessage,
            LogLevel logLevel = LogLevel.Info,
            Dictionary<string, string> customProperties = null)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            
            var result = action();
            
            LogTimingInformation(completionMessage, logger, logLevel, customProperties, stopwatch);
            stopwatch.Stop();
            return result;
        }

        /// <summary>
        /// Runs the provided action, tracking how long it takes to operate, then logs the elapsed time along with a provided message. 
        /// </summary>
        public static async Task<T> RunTimedAsync<T>(
            this ILogger logger,
            Func<Task<T>> action,
            string completionMessage,
            LogLevel logLevel = LogLevel.Info,
            Dictionary<string, string> customProperties = null)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            
            var result = await action();
            
            LogTimingInformation(completionMessage, logger, logLevel, customProperties, stopwatch);
            stopwatch.Stop();

            return result;
        }

        private static void LogTimingInformation(
            string completionMessage,
            ILogger logger,
            LogLevel logLevel,
            Dictionary<string, string> customProperties,
            Stopwatch stopwatch)
        {
            customProperties ??= new Dictionary<string, string>();
            customProperties["ExecutionTime"] = stopwatch.ElapsedMilliseconds.ToString();
            logger.SendTrace(completionMessage, logLevel, customProperties);
        }
    }
}