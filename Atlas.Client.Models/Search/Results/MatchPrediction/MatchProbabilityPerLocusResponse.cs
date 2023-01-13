using System;
using Atlas.Common.Public.Models.MatchPrediction;
using LocusMatchCategories = Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.LocusInfo<Atlas.Client.Models.Search.Results.MatchPrediction.PredictiveMatchCategory?>;

namespace Atlas.Client.Models.Search.Results.MatchPrediction
{
    public class MatchProbabilityPerLocusResponse
    {
        public MatchProbabilities MatchProbabilities { get; set; }

        public PredictiveMatchCategory? MatchCategory => MatchProbabilities.MatchCategory;

        /// <summary>
        /// Match grades are being assigned arbitrarily to pos1 and pos2.
        /// </summary>
        public LocusMatchCategories PositionalMatchCategories => MatchProbabilities.MatchCategory switch
        {
            PredictiveMatchCategory.Exact => new LocusMatchCategories(PredictiveMatchCategory.Exact),
            PredictiveMatchCategory.Mismatch => new LocusMatchCategories(PredictiveMatchCategory.Mismatch,
                GetSecondMatchCategoryWhenFirstIsMismatch()),
            PredictiveMatchCategory.Potential => new LocusMatchCategories(PredictiveMatchCategory.Potential,
                GetSecondMatchCategoryWhenFirstIsPotential()),
            null => null,
            _ => throw new ArgumentOutOfRangeException()
        };

        public MatchProbabilityPerLocusResponse()
        {
        }

        public MatchProbabilityPerLocusResponse(Probability sharedProbability)
        {
            MatchProbabilities = new MatchProbabilities(sharedProbability);
        }

        public MatchProbabilityPerLocusResponse(MatchProbabilities sharedMatchProbabilities)
        {
            MatchProbabilities = sharedMatchProbabilities;
        }

        public MatchProbabilityPerLocusResponse Round(int decimalPlaces)
        {
            return new MatchProbabilityPerLocusResponse
            {
                MatchProbabilities = MatchProbabilities?.Round(decimalPlaces)
            };
        }

        /// <summary>
        /// If first category is Mismatch,
        /// AND 100% chance of two mismatches, then other match category must be Mismatch.
        ///
        /// If first category is Mismatch,
        /// AND 0% chance of two mismatches, then other category must be Exact.
        /// </summary>
        /// <returns></returns>
        private PredictiveMatchCategory? GetSecondMatchCategoryWhenFirstIsMismatch()
        {
            return MatchProbabilities.TwoMismatchProbability.Percentage switch
            {
                100 => PredictiveMatchCategory.Mismatch,
                0 => PredictiveMatchCategory.Exact,
                _ => PredictiveMatchCategory.Potential
            };
        }

        /// <summary>
        /// If first category is Potential,
        /// And 0% chance of two mismatches, then other match category must be Exact.
        ///
        /// If first category is Potential,
        /// AND there is a chance of two mismatches, then neither of them can be Exact so match category is Potential.
        /// </summary>
        private PredictiveMatchCategory? GetSecondMatchCategoryWhenFirstIsPotential()
        {
            return MatchProbabilities.TwoMismatchProbability.Percentage == 0 ? PredictiveMatchCategory.Exact : PredictiveMatchCategory.Potential;
        }
    }
}