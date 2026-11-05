using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace Atlas.MatchingAlgorithm.Data.Helpers
{
    /// <summary>
    /// Runs a database write again after a transient SQL error: an error that a new attempt, on a new connection, can fix.
    /// </summary>
    public static class TransientSqlErrorRetry
    {
        /// <summary>
        /// The waits before the second, third and fourth attempts. A connection drop on Azure SQL usually clears in seconds.
        /// </summary>
        public static readonly IReadOnlyList<TimeSpan> DefaultRetryDelays = new[]
        {
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(15),
            TimeSpan.FromSeconds(30)
        };

        /// <summary>
        /// The transient errors of the SqlClient retry providers, and the network errors that they leave to the caller.
        /// The client-side command timeout (-2) is not here: the bulk insert timeouts are one to four hours, and a retry
        /// would only double that wait.
        /// </summary>
        private static readonly HashSet<int> TransientSqlErrorNumbers =
        [
            .. SqlConfigurableRetryFactory.BaselineTransientErrors,
            // The connection was lost: the network name is no longer available, a semaphore timeout, and connection resets.
            64, 121, 10053, 10054
        ];

        /// <summary>
        /// The lowest severity at which SQL Server closes the connection. "A transport-level error has occurred ...
        /// Physical connection is not usable" (seen on AN LIVE, ATL-524) has number -1 and severity 20, so it is matched
        /// by its severity, not by its number.
        /// </summary>
        private const byte ConnectionClosingSeverity = 20;

        /// <summary>
        /// Runs <paramref name="attempt"/>, and runs it again after a transient SQL error, up to one more time per delay.
        /// </summary>
        /// <param name="attempt">The write. Its argument is the attempt number, starting at 1.</param>
        /// <param name="onRetry">
        /// Called after a transient error and before the wait, with the error and the number of the attempt that failed.
        /// Use it to log the retry, and to drop anything that holds the broken connection.
        /// </param>
        /// <param name="retryDelays">The wait before each retry. The number of retries is the number of delays.</param>
        /// <param name="isTransient">Decides if an error is transient. Defaults to <see cref="IsTransient"/>.</param>
        public static async Task Run(
            Func<int, Task> attempt,
            Action<Exception, int> onRetry,
            IReadOnlyList<TimeSpan> retryDelays,
            Func<Exception, bool> isTransient = null)
        {
            isTransient ??= IsTransient;

            for (var attemptNumber = 1;; attemptNumber++)
            {
                try
                {
                    await attempt(attemptNumber);
                    return;
                }
                catch (Exception exception) when (attemptNumber <= retryDelays.Count && isTransient(exception))
                {
                    onRetry(exception, attemptNumber);
                    await Task.Delay(retryDelays[attemptNumber - 1]);
                }
            }
        }

        /// <summary>
        /// True for a SQL error that a new attempt on a new connection can fix, also when it is the inner exception.
        /// A constraint violation, a missing table or a full database is not transient.
        /// </summary>
        public static bool IsTransient(Exception exception) => exception switch
        {
            null => false,
            SqlException sqlException => sqlException.Errors.Cast<SqlError>().Any(error =>
                TransientSqlErrorNumbers.Contains(error.Number) || error.Class >= ConnectionClosingSeverity),
            AggregateException aggregate => aggregate.InnerExceptions.Any(IsTransient),
            _ => IsTransient(exception.InnerException)
        };
    }
}
