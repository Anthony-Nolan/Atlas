using System;

namespace Atlas.MatchingAlgorithm.Models
{
    public class MatchingAlgorithmSearchTrackingContext
    {
        public Guid SearchIdentifier { get; set; }
        public Guid? OriginalSearchIdentifier { get; set; }
        public byte AttemptNumber { get; set; }
    }
}
