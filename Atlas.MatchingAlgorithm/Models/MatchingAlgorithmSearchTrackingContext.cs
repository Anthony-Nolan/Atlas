using System;

namespace Atlas.MatchingAlgorithm.Models
{
    public class MatchingAlgorithmSearchTrackingContext
    {
        public Guid SearchRequestId { get; set; }
        public byte AttemptNumber { get; set; }
    }
}
