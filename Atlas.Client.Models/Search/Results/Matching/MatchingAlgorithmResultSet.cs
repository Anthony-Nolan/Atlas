using System.Collections.Generic;
using Atlas.Common.GeneticData.PhenotypeInfo.TransferModels;

namespace Atlas.Client.Models.Search.Results.Matching
{
    public class MatchingAlgorithmResultSet
    {
        public string SearchRequestId { get; set; }
        public string RepeatSearchId { get; set; }
        public bool IsRepeatSearchSet => RepeatSearchId != null;
        public int ResultCount { get; set; }
        public IEnumerable<MatchingAlgorithmResult> MatchingAlgorithmResults { get; set; }

        public string HlaNomenclatureVersion { get; set; }
        public string BlobStorageContainerName { get; set; }
        public string ResultsFileName => IsRepeatSearchSet ? $"{SearchRequestId}/{RepeatSearchId}.json":  $"{SearchRequestId}.json";
        
        /// <summary>
        /// The HLA that the search was run against.
        /// </summary>
        public PhenotypeInfoTransfer<string> SearchedHla { get; set; }
    }
}