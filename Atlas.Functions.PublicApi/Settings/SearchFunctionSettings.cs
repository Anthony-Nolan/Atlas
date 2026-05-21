namespace Atlas.Functions.PublicApi.Settings
{
    public class SearchFunctionSettings
    {
        /// <summary>
        /// Applied to <see cref="Atlas.Client.Models.Search.Requests.SearchRequest.ParallelMatchPrediction"/> when the caller does not specify a value.
        /// Set to <c>true</c> for DEV; <c>false</c> for UAT/Production until the parallel path is fully validated.
        /// </summary>
        public bool DefaultParallelMatchPrediction { get; set; }
    }
}
