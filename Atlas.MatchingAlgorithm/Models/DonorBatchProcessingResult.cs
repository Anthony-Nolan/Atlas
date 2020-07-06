using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Atlas.MatchingAlgorithm.Models
{
    public class DonorBatchProcessingResult<T>
    {
        public DonorBatchProcessingResult(IEnumerable<T> results = null)
        {
            ProcessingResults = (results?.ToList() ?? new List<T>()).AsReadOnly();
        }

        // We insist that this are ReadOnlyCollections so that we can be sure that all the Processing is complete before we start doing anything with the results.
        // This makes it easier to be certain that we are persisting the processing results atomically. (Which in turn ensures that the messages are correctly <removed from / left on> the queues

        public ReadOnlyCollection<T> ProcessingResults { get; set; }
        public ReadOnlyCollection<FailedDonorInfo> FailedDonors { get; set; } = new List<FailedDonorInfo>().AsReadOnly();
    }
}
