﻿using System;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Utils.Models;

namespace Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability
{
    public class MatchProbabilityPerLocusResponse
    {
        public MatchProbabilities MatchProbabilities { get; set; }

        /// <summary>
        /// Match grades are being assigned arbitrarily to pos1 and pos2.
        /// </summary>
        public LocusInfo<PredictiveMatchCategory> PositionalMatchCategories => MatchProbabilities.MatchCategory switch
        {
            PredictiveMatchCategory.Exact => new LocusInfo<PredictiveMatchCategory>(PredictiveMatchCategory.Exact),
            PredictiveMatchCategory.Mismatch => new LocusInfo<PredictiveMatchCategory>(PredictiveMatchCategory.Mismatch, GetMismatchSecondMatchCategory()),
            PredictiveMatchCategory.Potential => new LocusInfo<PredictiveMatchCategory>(PredictiveMatchCategory.Potential, GetPotentialSecondMatchCategory()),
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
        /// If zero percent chance of two mismatches, other match category must be exact.
        /// If zero percent chance of one mismatch, can't be either exact or potential so must be mismatch.
        /// All other cases the match category will be potential.
        /// </summary>
        private PredictiveMatchCategory GetMismatchSecondMatchCategory()
        {
            if (MatchProbabilities.TwoMismatchProbability.Decimal == 0)
            {
                return PredictiveMatchCategory.Exact;
            }

            if (MatchProbabilities.OneMismatchProbability.Decimal == 0)
            {
                return PredictiveMatchCategory.Mismatch;
            }

            return PredictiveMatchCategory.Potential;
        }

        /// <summary>
        /// If zero percent chance of two mismatches, other match category must be exact.
        /// If There is a chance of two mismatches then neither of them can be exact so match category is potential.
        /// </summary>
        private PredictiveMatchCategory GetPotentialSecondMatchCategory()
        {
            return MatchProbabilities.TwoMismatchProbability.Decimal == 0 ? PredictiveMatchCategory.Exact : PredictiveMatchCategory.Potential;
        }
    }
}