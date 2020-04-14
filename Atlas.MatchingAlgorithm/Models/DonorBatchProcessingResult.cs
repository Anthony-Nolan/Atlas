using System.Collections.Generic;

namespace Atlas.MatchingAlgorithm.Models
{
    public class DonorBatchProcessingResult<T>
    {
        public IEnumerable<T> ProcessingResults { get; set; } = new List<T>();
        public IEnumerable<FailedDonorInfo> FailedDonors { get; set; } = new List<FailedDonorInfo>();
    }
}
