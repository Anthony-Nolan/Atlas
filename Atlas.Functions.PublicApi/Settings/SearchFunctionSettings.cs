namespace Atlas.Functions.PublicApi.Settings
{
    public class SearchFunctionSettings
    {
        /// <summary>
        /// Master switch for the parallel ACA Worker ("Containers") match-prediction path. When <c>false</c>, every
        /// search takes the legacy sequential Durable orchestrator path, regardless of
        /// <see cref="ParallelMatchPredictionRequestPercentage"/>. When <c>true</c>, that percentage of searches take
        /// the parallel path. The value supplied on the request is not used — routing is controlled entirely by these
        /// two settings. Set to <c>true</c> for DEV; <c>false</c> for UAT/Production until the parallel path is fully validated.
        /// </summary>
        public bool DefaultParallelMatchPrediction { get; set; }

        /// <summary>
        /// Canary throttle (0-100) applied only when <see cref="DefaultParallelMatchPrediction"/> is <c>true</c>: this
        /// percentage of searches take the parallel ACA Worker ("Containers") path and the remainder fall back to the
        /// legacy sequential Durable orchestrator path. When <see cref="DefaultParallelMatchPrediction"/> is <c>false</c>
        /// the percentage has no effect. Defaults to <c>100</c> (all parallel when the master switch is on). Values
        /// outside 0-100 are clamped.
        /// </summary>
        public int ParallelMatchPredictionRequestPercentage { get; set; } = 100;
    }
}
