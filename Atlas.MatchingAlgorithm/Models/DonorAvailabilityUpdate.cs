using Atlas.MatchingAlgorithm.Data.Models.DonorInfo;
using System;

namespace Atlas.MatchingAlgorithm.Models
{
    public class DonorAvailabilityUpdate
    {
        public long UpdateSequenceNumber { get; set; }
        public DateTimeOffset UpdateDateTime { get; set; }
        public int DonorId { get; set; }
        public bool IsAvailableForSearch { get; set; }
        public DonorInfo DonorInfo { get; set; }
    }
}