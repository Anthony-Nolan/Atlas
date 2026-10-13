using Atlas.Common.Public.Models.MatchPrediction;

namespace Atlas.MatchPrediction.Functions.Models.Debug
{
    internal class GenotypeImputationRequest
    {
        public SubjectInfo SubjectInfo { get; set; }

        /// <summary>
        /// <inheritdoc cref="Common.Public.Models.MatchPrediction.MatchPredictionParameters"/>
        /// </summary>
        public MatchPredictionParameters MatchPredictionParameters { get; set; }
    }
}