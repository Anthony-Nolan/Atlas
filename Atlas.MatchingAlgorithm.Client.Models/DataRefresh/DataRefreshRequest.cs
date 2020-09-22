namespace Atlas.MatchingAlgorithm.Client.Models.DataRefresh
{
    public class DataRefreshRequest
    {
        /// <summary>
        /// If true, the refresh will occur regardless of whether a new HLA Nomenclature version has been published.
        /// </summary>
        public bool ForceDataRefresh { get; set; }
    }
}