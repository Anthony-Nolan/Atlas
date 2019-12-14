using System.Collections.Generic;

namespace Nova.SearchAlgorithm.Models
{
    public class DonorBatchProcessingResult<T>
    {
        public IEnumerable<T> ProcessingResults { get; set; } = new List<T>();
        public IEnumerable<FailedDonorInfo> FailedDonors { get; set; } = new List<FailedDonorInfo>();
    }
}
