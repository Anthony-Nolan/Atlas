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

        /// <summary>
        /// The switch for precomputed donor genotype sets in match prediction (ATL-535). It applies only to requests that
        /// leave <c>UsePrecomputedGenotypeSets</c> empty; a request that sets the value is always kept. When <c>false</c>
        /// (the default), every such search calculates live. When <c>true</c>,
        /// <see cref="UsePrecomputedGenotypeSetsRequestPercentage"/> of such searches use the stored sets.
        /// </summary>
        /// <remarks>
        /// With <see cref="UsePrecomputedGenotypeSetsRequestPercentage"/>, this is the kill switch of the precomputed path.
        /// It is read when a search is accepted, so a change affects new searches only: a running search keeps its method.
        /// </remarks>
        public bool DefaultUsePrecomputedGenotypeSets { get; set; }

        /// <summary>
        /// The share (0-100) of searches that use precomputed donor genotype sets, when
        /// <see cref="DefaultUsePrecomputedGenotypeSets"/> is <c>true</c> and the request leaves
        /// <c>UsePrecomputedGenotypeSets</c> empty. The rest calculate live. Defaults to <c>0</c>, so a share of 0 is also
        /// off. Values outside 0-100 are clamped.
        /// </summary>
        public int UsePrecomputedGenotypeSetsRequestPercentage { get; set; }
    }
}
