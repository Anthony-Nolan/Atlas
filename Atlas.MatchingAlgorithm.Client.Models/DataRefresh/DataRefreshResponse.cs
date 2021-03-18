namespace Atlas.MatchingAlgorithm.Client.Models.DataRefresh
{
    public class DataRefreshResponse
    {
        public int? DataRefreshRecordId { get; set; }
        public bool WasRefreshRun { get; set; } = true;
    }
}
