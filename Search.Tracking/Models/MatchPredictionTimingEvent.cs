﻿namespace Atlas.SearchTracking.Models
{
    public class MatchPredictionTimingEvent
    {
        public int SearchRequestId { get; set; }
        public DateTime TimeUtc { get; set; }
    }
}