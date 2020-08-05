using System;

namespace Atlas.Functions.Models
{
    public class TimedResultSet<TResultSet>
    {
        /// <summary>
        /// How long it took to generate a result set.
        /// e.g. elapsed time of an algorithm
        /// </summary>
        public TimeSpan ElapsedTime { get; set; }
        
        /// <summary>
        /// Store time the timed set was created.
        /// This is useful for calculating timings in Durable Functions, where we cannot use stopwatches.
        /// </summary>
        public DateTime? FinishedTimeUtc { get; set; }
        
        /// <summary>
        /// Results of the operation.
        /// </summary>
        public TResultSet ResultSet { get; set; }
    }
}