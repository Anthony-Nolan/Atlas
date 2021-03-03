using System;
using System.Collections.Generic;
using Atlas.Common.GeneticData.PhenotypeInfo.TransferModels;

// ReSharper disable MemberCanBeInternal
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Atlas.Client.Models.Search.Results
{
    public class SearchResultSet
    {
        public string SearchRequestId { get; set; }

        /// <summary>
        /// If a repeat search, distinguishes this particular run of the repeat search.
        /// For first time searches, this will be null.
        /// </summary>
        public string RepeatSearchId { get; set; }

        public bool IsRepeatSearchSet => RepeatSearchId != null;

        public int TotalResults { get; set; }
        public IEnumerable<SearchResult> SearchResults { get; set; }

        public string HlaNomenclatureVersion { get; set; }
        public string BlobStorageContainerName { get; set; }
        public string ResultsFileName => IsRepeatSearchSet ? $"{SearchRequestId}/{RepeatSearchId}.json" : $"{SearchRequestId}.json";

        public TimeSpan MatchingAlgorithmTime { get; set; }
        public TimeSpan MatchPredictionTime { get; set; }
        
        public PhenotypeInfoTransfer<string> SearchedHla { get; set; }
    }
}