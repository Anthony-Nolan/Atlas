/* ******************************
   **  Copyright Softwire 2020 ** 
   ****************************** */
using System;

namespace LoggingStopwatch
{
    /// <summary>
    /// Defines
    ///     <see cref="ExpectedNumberOfIterations"/>,
    ///     <see cref="InnerOperationLoggingPeriod"/>,
    ///     <see cref="ReportOuterTimerStart"/>
    ///     <see cref="ReportPercentageCompletion"/>,
    ///     <see cref="ReportProjectedCompletionTime"/>,
    ///     <see cref="ReportThreadCount"/>
    /// and
    ///     <see cref="ReportPerThreadTime"/>
    /// </summary>
    public class LongLoggingSettings
    {
        /// <summary>
        /// How many times do we expect the inner loop to be called?
        /// Getting this prediction wrong won't cause any problems,
        /// it will just lead to the percentage and Completion Time reports
        /// being inaccurate (if used).
        /// </summary>
        public int? ExpectedNumberOfIterations { get; set; }

        /// <summary>How frequently should the progress of inner operations be logged. default is 1, i.e. after every loop of the inner operation</summary>
        public int InnerOperationLoggingPeriod { get; set; } = 1;

        /// <summary>Logs a record when the outer timer is initialised. <c>True</c> by default</summary>
        public bool ReportOuterTimerStart { get; set; } = true;

        /// <summary>Reports what proportion of the Expected iterations have been completed. <c>True</c> by default</summary>
        public bool ReportPercentageCompletion { get; set; } = true;

        /// <summary>Reports a simple linear extrapolation of the completion time of the overall process. <c>True</c> by default</summary>
        public bool ReportProjectedCompletionTime { get; set; } = true;

        /// <summary>Reports on how many distinct threads were utilised.<c>False</c> by default</summary>
        public bool ReportThreadCount { get; set; } = false;

        /// <summary>Reports the final distribution of time on various different threads. <c>False</c> by default</summary>
        public bool ReportPerThreadTime { get; set; } = false;

        internal void Validate()
        {
            if (ExpectedNumberOfIterations == null &&
                (ReportPercentageCompletion || ReportProjectedCompletionTime))
            {
                throw new ArgumentException($"{nameof(ReportPercentageCompletion)} and {nameof(ReportProjectedCompletionTime)} require {nameof(ExpectedNumberOfIterations)} to be provided.");
            }

            if (ExpectedNumberOfIterations <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(ExpectedNumberOfIterations), $"{nameof(ExpectedNumberOfIterations)} must be strictly positive! (Received '{ExpectedNumberOfIterations}')");
            }

            if (ReportPerThreadTime && !ReportThreadCount)
            {
                ReportThreadCount = true;
            }

        }
    }
}
