using System;
using Atlas.Client.Models.Search.Requests;

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Atlas.Client.Models.Search.Results.Matching
{
    public class MatchingResultsNotification
    {
        /// <summary>
        /// Include full search request details in results notification, as match prediction will need it to run,
        /// which is triggered by this notification.
        /// </summary>
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

        /// <summary>
        /// Name of the container in blob storage where matching results can be found.
        /// </summary>
        public string BlobStorageContainerName { get; set; }

        /// <summary>
        /// Name of the file in which results are stored in blob storage. 
        /// </summary>
        public string BlobStorageResultsFileName { get; set; }
        
        public TimeSpan ElapsedTime { get; set; }
    }
}