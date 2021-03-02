namespace Atlas.MatchingAlgorithm.Client.Models.DataRefresh
{
    public class DataRefreshRequest
    {
        /// <summary>
        /// If true, the refresh will occur regardless of whether a new HLA Nomenclature version has been published.
        /// Otherwise, the refresh will only run if a new nomenclature version is detected.
        /// </summary>
        public bool ForceDataRefresh { get; set; }
    }
}