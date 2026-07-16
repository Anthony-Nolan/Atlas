namespace Atlas.Functions.PublicApi.Settings
{
    public class SearchFunctionSettings
    {
        /// <summary>
        /// Applied to <see cref="Atlas.Client.Models.Search.Requests.SearchRequest.ParallelMatchPrediction"/> when the caller does not specify a value.
        /// Set to <c>true</c> for DEV; <c>false</c> for UAT/Production until the parallel path is fully validated.
        /// </summary>
        public bool DefaultParallelMatchPrediction { get; set; }

        /// <summary>
        /// Canary throttle for the parallel ACA Worker ("Containers") match-prediction path, expressed as a whole
        /// percentage (0-100). Once <see cref="Atlas.Client.Models.Search.Requests.SearchRequest.ParallelMatchPrediction"/>
        /// has resolved to <c>true</c> (either explicitly or via <see cref="DefaultParallelMatchPrediction"/>), only this
        /// percentage of those requests actually take the parallel path; the remainder fall back to the legacy sequential
        /// Durable orchestrator path. It never promotes a request that resolved to <c>false</c> onto the parallel path.
        /// Defaults to <c>100</c> (every parallel-resolved request takes the parallel path — i.e. no throttling), so
        /// existing behaviour is preserved unless the value is deliberately lowered. Values outside 0-100 are clamped.
        /// </summary>
        public int ParallelMatchPredictionRequestPercentage { get; set; } = 100;
    }
}
