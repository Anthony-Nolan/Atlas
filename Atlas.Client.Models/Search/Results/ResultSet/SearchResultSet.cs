using System;
using Atlas.Common.GeneticData.PhenotypeInfo.TransferModels;

// ReSharper disable MemberCanBeInternal
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Atlas.Client.Models.Search.Results.ResultSet
{
    public abstract class SearchResultSet : ResultSet<SearchResult>
    {
        public string SearchRequestId { get; set; }

        public abstract bool IsRepeatSearchSet { get; }

        public string HlaNomenclatureVersion { get; set; }
        public string BlobStorageContainerName { get; set; }
        public abstract string ResultsFileName { get; }

        public TimeSpan MatchingAlgorithmTime { get; set; }
        public TimeSpan MatchPredictionTime { get; set; }
        
        public PhenotypeInfoTransfer<string> SearchedHla { get; set; }
    }
}