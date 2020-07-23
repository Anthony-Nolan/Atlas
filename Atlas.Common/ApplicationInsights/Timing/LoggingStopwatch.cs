/* ******************************
   **  Copyright Softwire 2020 ** 
   ****************************** */
using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace LoggingStopwatch
{
    /// <summary>
    /// Starts a System.Diagnostics.Stopwatch on construction, and logs the elapsed time when `.Dispose()` is called.
    /// Thus <c>SomeMethod()</c> can be timed by calling:
    ///
    /// <code>
    ///     using(new LoggingStopwatch("SomeMethod", logger))
    ///     {
    ///         SomeMethod();
    ///     }//Log will be written here.
    /// </code>
    /// Also accepts anonymous lambda as a logger: <c>new LoggingStopwatch("AnotherMethod", (text) => myLogger.WriteLog(text))</c>
    /// <br/>
    /// If you are running a loop with multiple iterations, you may want to use the more complex <see cref="LongOperationLoggingStopwatch"/>
    /// </summary>
    public class LoggingStopwatch : IDisposable
    {
        private readonly string identifier;
        private readonly string uniqueId;
        private readonly IStopwatchLogger logger;
        protected readonly Stopwatch timer = new Stopwatch();

        #region Constructors
        /// <summary>
        /// Starts a System.Diagnostics.Stopwatch on construction, and logs the elapsed time when `.Dispose()` is called.
        /// </summary>
        /// <param name="identifier">
        /// String to identify which operation was timed, in the logs.
        /// A random unique identifier will be generated in addition to this, so that multiple executions are distinguishable.
        /// </param>
        /// <param name="logger">
        /// Logger object to perform the logging when appropriate. 
        /// </param>
        /// <param name="logAtStart">
        /// Indicates whether a log record should be written at the start of the process.
        /// Consider using the <see cref="LongOperationLoggingStopwatch"/> if you want this.
        /// </param>
        public LoggingStopwatch(string identifier, IStopwatchLogger logger, bool logAtStart = false)
        {
            this.logger = logger;
            this.identifier = identifier;
            //Ensure that if we time the same operation repeatedly, we can tell which logs are associated with which execution.
            uniqueId = GenerateRandomStringId();
            if (logAtStart)
            {
                Log("Started.");
            }
            timer.Start();
        }

        /// <inheritdoc/>
        /// <param name="identifier">Defers to Inherited paramDoc</param>
        /// <param name="loggingAction">Method to call whenever some text should be logged.</param>
        /// <param name="logAtStart">Defers to Inherited paramDoc</param>
        public LoggingStopwatch(string identifier, Action<string> loggingAction, bool logAtStart = false)
            : this(identifier, new LambdaLogger(loggingAction), logAtStart)
        { }

        //Note if you copy-paste this code, feel free to delete this if you don't want the Microsoft.Extensions.Logging dependency.
        /// <inheritdoc/>
        /// <param name="identifier">Defers to Inherited paramDoc</param>
        /// <param name="microsoftLogger">Accepts an <see cref="ILogger"/> and logs to it at the <see cref="LogLevel.Information"/> level.</param>
        /// <param name="logAtStart">Defers to Inherited paramDoc</param>
        public LoggingStopwatch(string identifier, ILogger microsoftLogger, bool logAtStart = false)
            : this(identifier, new MicrosoftLoggerWrapper(microsoftLogger), logAtStart)
        { }
        #endregion

        protected void Log(string text)
        {
            logger.Log($"{identifier}|{uniqueId}|{text}");
        }

        public virtual void Dispose()
        {
            //TODO: What about errors throw in the using block.
            Log($"Completed in: {timer.Elapsed}");
        }

        private static readonly Random RandomSource = new Random();
        private static string GenerateRandomStringId()
        {
            var length = 5;
            const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            var output = new char[length];
            for (int i = 0; i < length; i++)
            {
                var choice = RandomSource.Next(alphabet.Length);
                output[i] = alphabet[choice];
            }
            return new string(output);
        }
    }
}
