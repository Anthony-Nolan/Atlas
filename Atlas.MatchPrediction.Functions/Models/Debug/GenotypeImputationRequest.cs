using Atlas.MatchPrediction.Models;

namespace Atlas.MatchPrediction.Functions.Models.Debug
{
    public class GenotypeImputationRequest
    {
        public SubjectInfo SubjectInfo { get; set; }

        /// <summary>
        /// <inheritdoc cref="MatchPrediction.Models.MatchPredictionParameters"/>
        /// </summary>
        public MatchPredictionParameters MatchPredictionParameters { get; set; }
    }
}