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
        /// Name of the file where results are stored in blob storage.
        /// When <see cref="ResultsBatched" /> is true, it contains only search summary, results are stored in a folder <see cref="BatchFolderName" />
        /// </summary>
        public string ResultsFileName { get; set; }

        public int? NumberOfResults { get; set; }

        /// <summary>
        /// The version of the HLA Nomenclature used by the matching algorithm component - used for analysing both donor and patient hla.
        /// </summary>
        public string MatchingAlgorithmHlaNomenclatureVersion { get; set; }

        /// <summary>
        /// Indicates if results were batched (i.e. saved in multiple files separately from the search summary) or not
        /// </summary>
        public bool ResultsBatched { get; set; }

        /// <summary>
        /// Name of the folder in blob storage where files with results are stored. It's populated only when <see cref="ResultsBatched" /> is true and <see cref="NumberOfResults" /> is greater than 0, otherwise it will be null
        /// </summary>
        public string BatchFolderName { get; set; }
    }
}