using System;

namespace Atlas.MatchingAlgorithm.Data.Models
{
    public class DonorManagementInfo
    {
        public int DonorId { get; set; }
        public long UpdateSequenceNumber { get; set; }
        public DateTimeOffset UpdateDateTime { get; set; }
    }
}
