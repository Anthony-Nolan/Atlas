using Atlas.Common.Public.Models.MatchPrediction;

namespace Atlas.MatchPrediction.Functions.Models.Debug
{
    internal class GenotypeMatcherRequest
    {
        /// <summary>
        /// <inheritdoc cref="Common.Public.Models.MatchPrediction.MatchPredictionParameters"/> 
        /// </summary>
        public MatchPredictionParameters MatchPredictionParameters { get; set; }

        public SubjectInfo Patient { get; set; }
        public SubjectInfo Donor { get; set; }
    }
}