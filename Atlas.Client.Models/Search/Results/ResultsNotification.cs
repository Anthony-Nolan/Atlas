namespace Atlas.Client.Models.Search.Results
{
    public abstract class ResultsNotification
    {
        public string SearchRequestId { get; set; }
        public bool WasSuccessful { get; set; }
        
        /// <summary>
        /// If a repeat search, distinguishes this particular run of the repeat search.
        /// For first time searches, this will be null.
        /// </summary>
        public string RepeatSearchRequestId { get; set; }

        /// <summary>
        /// Name of the container in blob storage where results can be found.
        /// </summary>
        public string BlobStorageContainerName { get; set; }

        /// <summary>
        /// Name of the file in which results are stored in blob storage. 
        /// </summary>
        public string ResultsFileName { get; set; }

        public int? NumberOfResults { get; set; }

        /// <summary>
        /// The version of the HLA Nomenclature used by the matching algorithm component - used for analysing both donor and patient hla.
        /// </summary>
        public string MatchingAlgorithmHlaNomenclatureVersion { get; set; }

        public bool ResultBatched { get; set; }

        public string BatchFolder { get; set; }
    }
}