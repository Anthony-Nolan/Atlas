using System;

namespace Atlas.MatchingAlgorithm.Data.Models
{
    public class DonorManagementInfo
    {
        public int DonorId { get; set; }
        
        // TODO: ATLAS-972: Confirm this is unused and remove
        public long UpdateSequenceNumber { get; set; }
        public DateTimeOffset UpdateDateTime { get; set; }
    }
}
