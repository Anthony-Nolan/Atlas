using System.Collections.Generic;
using Atlas.Common.ApplicationInsights;

namespace Atlas.DonorImport.ApplicationInsights
{
    internal class SearchableDonorValidationErrorEventModel : EventModel
    {
        public SearchableDonorValidationErrorEventModel(string errorMessage, IEnumerable<string> failedDonorIds) : base("Searchable Donor Validation Error")
        {
            Level = LogLevel.Warn;
            Properties.Add(nameof(errorMessage), errorMessage);
            Properties.Add(nameof(failedDonorIds), string.Join(",", failedDonorIds));
        }
    }
}