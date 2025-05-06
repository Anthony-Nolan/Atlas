namespace Atlas.Debug.Client.Models.SearchTracking
{
    public class SearchTrackingMatchPredictionInfo
    {
        public bool? IsSuccessful { get; set; }

        public SearchTrackingMatchPredictionFailureInfo FailureInfo { get; set; }

        public int? DonorsPerBatch { get; set; }

        public int? TotalNumberOfBatches { get; set; }
    }
}
