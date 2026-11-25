namespace Atlas.Functions.PublicApi.Settings
{
    public class SearchFunctionSettings
    {
        /// <summary>
        /// The switch for the parallel ACA Worker ("Containers") match prediction path. It applies only to requests that
        /// leave <c>ParallelMatchPrediction</c> empty (<c>null</c>); a request that sets the value is always kept. When
        /// <c>true</c>, such a request takes the parallel path. When <c>false</c> (the default), it takes the legacy
        /// sequential Durable orchestrator path.
        /// </summary>
        public bool DefaultParallelMatchPrediction { get; set; }

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
