using Atlas.MatchPrediction.Models;

namespace Atlas.MatchPrediction.Functions.Models.Debug
{
    public class GenotypeMatcherRequest
    {
        /// <summary>
        /// <inheritdoc cref="MatchPrediction.Models.MatchPredictionParameters"/> 
        /// </summary>
        public MatchPredictionParameters MatchPredictionParameters { get; set; }

        public SubjectInfo Patient { get; set; }
        public SubjectInfo Donor { get; set; }
    }
}