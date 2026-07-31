namespace Atlas.Functions.PublicApi.Settings
{
    public class SearchFunctionSettings
    {
        /// <summary>
        /// Default master switch for the parallel ACA Worker ("Containers") match-prediction path, applied only to
        /// requests that leave <c>ParallelMatchPrediction</c> unset (<c>null</c>); a request that sets the flag
        /// explicitly is always honoured and ignores both settings. When <c>false</c>, an unset request takes the
        /// legacy sequential Durable orchestrator path, regardless of <see cref="ParallelMatchPredictionRequestPercentage"/>.
        /// When <c>true</c>, that percentage of unset requests take the parallel path. Set to <c>true</c> for DEV;
        /// <c>false</c> for UAT/Production until the parallel path is fully validated.
        /// </summary>
        public bool DefaultParallelMatchPrediction { get; set; }

        /// <summary>
        /// Canary throttle (0-100) applied only to requests that leave <c>ParallelMatchPrediction</c> unset
        /// (<c>null</c>) and only when <see cref="DefaultParallelMatchPrediction"/> is <c>true</c>: this percentage of
        /// such searches take the parallel ACA Worker ("Containers") path and the remainder fall back to the legacy
        /// sequential Durable orchestrator path. When <see cref="DefaultParallelMatchPrediction"/> is <c>false</c>, or
        /// the request sets the flag explicitly, the percentage has no effect. Defaults to <c>100</c> (all parallel when
        /// the master switch is on). Values outside 0-100 are clamped.
        /// </summary>
        public int ParallelMatchPredictionRequestPercentage { get; set; } = 100;
    }
}
