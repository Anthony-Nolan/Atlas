using System;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.TransferModels;
using Atlas.Common.Public.Models.MatchPrediction;
using Newtonsoft.Json;
using LocusMatchCategories = Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.LocusInfo<Atlas.Client.Models.Search.Results.MatchPrediction.PredictiveMatchCategory?>;

namespace Atlas.Client.Models.Search.Results.MatchPrediction
{
    public class MatchProbabilityPerLocusResponse
    {
        public MatchProbabilities MatchProbabilities { get; set; }

        public PredictiveMatchCategory? MatchCategory => MatchProbabilities.MatchCategory;

        [JsonIgnore]
        public LocusMatchCategories PositionalMatchCategories { get; set; }

        /// <summary>
        /// Used when serializing and deserializing <see cref="PositionalMatchCategories"/>
        /// </summary>
        [JsonProperty(nameof(PositionalMatchCategories))]
        public LocusInfoTransfer<PredictiveMatchCategory?> PositionalMatchCategoriesTransfer
        {
            get => PositionalMatchCategories?.ToLocusInfoTransfer();
            set => PositionalMatchCategories = value?.ToLocusInfo();
        }

        #region Constructors

        [JsonConstructor]
        public MatchProbabilityPerLocusResponse()
        {
        }

        /// <summary>
        /// Sets <see cref="MatchProbabilities"/>, and also <see cref="PositionalMatchCategories"/> as a function of <see cref="MatchProbabilities"/>.
        /// </summary>
        public MatchProbabilityPerLocusResponse(MatchProbabilities matchProbabilities)
        {
            MatchProbabilities = matchProbabilities;
            PositionalMatchCategories = CalculatePositionalMatchCategories();
        }

        /// <summary>
        /// Initialises all probabilities within <see cref="MatchProbabilities"/> with the same <paramref name="probability"/> value.
        /// <inheritdoc cref="MatchProbabilityPerLocusResponse(MatchProbabilities)"/>
        /// </summary>
        public MatchProbabilityPerLocusResponse(Probability probability) : this(new MatchProbabilities(probability))
        {
        }

        #endregion

        public MatchProbabilityPerLocusResponse Round(int decimalPlaces)
        {
            return new MatchProbabilityPerLocusResponse(MatchProbabilities?.Round(decimalPlaces));
        }

        /// <returns>Predictive match categories for each position, assigned arbitrarily to pos1 and pos2.</returns>
        private LocusMatchCategories CalculatePositionalMatchCategories()
        {
            return MatchProbabilities.MatchCategory switch
            {
                PredictiveMatchCategory.Exact => new LocusMatchCategories(PredictiveMatchCategory.Exact),
                PredictiveMatchCategory.Mismatch => new LocusMatchCategories(PredictiveMatchCategory.Mismatch,
                    GetSecondMatchCategoryWhenFirstIsMismatch()),
                PredictiveMatchCategory.Potential => new LocusMatchCategories(PredictiveMatchCategory.Potential,
                    GetSecondMatchCategoryWhenFirstIsPotential()),
                null => null,
                _ => throw new ArgumentOutOfRangeException()
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