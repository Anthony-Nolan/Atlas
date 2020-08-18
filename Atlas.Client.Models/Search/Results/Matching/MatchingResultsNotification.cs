using Atlas.Client.Models.Search.Requests;

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Atlas.Client.Models.Search.Results.Matching
{
    public class MatchingResultsNotification
    {
        public SearchRequest SearchRequest { get; set; }

        public string SearchRequestId { get; set; }
        public bool WasSuccessful { get; set; }
        public int? NumberOfResults { get; set; }

        /// <summary>
        ///     The version of the deployed search algorithm that ran the search request
        /// </summary>
        public string SearchAlgorithmServiceVersion { get; set; }

        /// <summary>
        ///     The version of the HLA Nomenclature used to run the search request - used for analysing both donor and patient hla.
        /// </summary>
        public string HlaNomenclatureVersion { get; set; }

        public string BlobStorageContainerName { get; set; }
        public long SearchTimeInMilliseconds { get; set; }
    }
}