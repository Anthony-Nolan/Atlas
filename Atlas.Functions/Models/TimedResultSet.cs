using System;

namespace Atlas.Functions.Models
{
    public class TimedResultSet<TResultSet>
    {
        public TimeSpan ElapsedTime { get; set; }
        public int ElapsedTimeMilliseconds => ElapsedTime.Milliseconds;
        public TResultSet ResultSet { get; set; }
    }
}