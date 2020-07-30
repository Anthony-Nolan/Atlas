/* ******************************
   **  Copyright Softwire 2020 ** 
   ****************************** */
// This was taken from a Softwire shareable Repo. At soem point it may get nugetified, in which case we might want
// migrate to that. Worth checking whether we've diverged, from the original code, though.

using System;
using System.Diagnostics;

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
        private readonly Action<string, long?> loggingAction;
        protected readonly Stopwatch Timer = new Stopwatch();

        /// <summary>
        /// Starts a System.Diagnostics.Stopwatch on construction, and logs the elapsed time when `.Dispose()` is called.
        /// </summary>
        /// <param name="identifier">
        /// String to identify which operation was timed, in the logs.
        /// A random unique identifier will be generated in addition to this, so that multiple executions are distinguishable.
        /// </param>
        /// <param name="loggingAction">
        /// Method to call whenever some text should be logged.
        /// Accepts string to log directly, and elapsed milliseconds to log as property.
        /// </param>
        /// <param name="logAtStart">
        /// Indicates whether a log record should be written at the start of the process.
        /// Consider using the <see cref="LongOperationLoggingStopwatch"/> if you want this.
        /// </param>
        public LoggingStopwatch(string identifier, Action<string, long?> loggingAction, bool logAtStart = false)
        {
            this.loggingAction = loggingAction;
            this.identifier = identifier;
            //Ensure that if we time the same operation repeatedly, we can tell which logs are associated with which execution.
            uniqueId = GenerateRandomStringId();
            if (logAtStart)
            {
                Log("Started.");
            }

            Timer.Start();
        }
        
        protected void Log(string text, long? elapsedMilliseconds = null)
        {
            loggingAction($"{identifier} |{uniqueId}| {text}", elapsedMilliseconds);
        }

        public virtual void Dispose()
        {
            //TODO: What about errors throw in the using block.
            Log($"Completed in: {Timer.Elapsed}", Timer.ElapsedMilliseconds);
        }

        private static readonly Random RandomSource = new Random();

        private static string GenerateRandomStringId()
        {
            const int length = 5;
            // ReSharper disable once StringLiteralTypo
            const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            var output = new char[length];
            for (var i = 0; i < length; i++)
            {
                var choice = RandomSource.Next(alphabet.Length);
                output[i] = alphabet[choice];
            }

            return new string(output);
        }
    }
}