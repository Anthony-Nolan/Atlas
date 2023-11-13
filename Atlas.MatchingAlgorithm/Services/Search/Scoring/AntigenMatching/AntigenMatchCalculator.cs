using System;
using System.Linq;
using Atlas.Client.Models.Search.Results.Matching.PerLocus;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata.ScoringMetadata;

namespace Atlas.MatchingAlgorithm.Services.Search.Scoring.AntigenMatching
{
    internal interface IAntigenMatchCalculator
    {
        /// <summary>
        /// Are <paramref name="patientMetadata"/> and <paramref name="donorMetadata"/> antigen matched?
        /// <inheritdoc cref="LocusPositionScoreDetails.IsAntigenMatch"/>
        /// </summary>
        bool? IsAntigenMatch(IHlaScoringMetadata patientMetadata, IHlaScoringMetadata donorMetadata);
    }

    internal class AntigenMatchCalculator : IAntigenMatchCalculator
    {
        /// <inheritdoc />
        public bool? IsAntigenMatch(IHlaScoringMetadata patientMetadata, IHlaScoringMetadata donorMetadata)
        {
            if (patientMetadata is null || donorMetadata is null)
            {
                return null;
            }

            if (patientMetadata.Locus != donorMetadata.Locus)
            {
                throw new ArgumentException($"Cannot calculate antigen match as patient typing is from locus {patientMetadata.Locus} and donor typing is from locus {donorMetadata.Locus}.");
            }

            return ArePatientAndDonorAntigenMatched(patientMetadata, donorMetadata);
        }

        private static bool ArePatientAndDonorAntigenMatched(IHlaScoringMetadata patientMetadata, IHlaScoringMetadata donorMetadata)
        {
            var patientMatchingSerologies = patientMetadata.HlaScoringInfo.MatchingSerologies.Select(s => s.Name).ToList();
            var donorSerologyEquivalents = donorMetadata.HlaScoringInfo.MatchingSerologies.Where(s => s.IsDirectMapping).Select(s => s.Name).ToList();

            // if both typings are allele-matched, expressing, molecular typings, then early return `true`
            bool IsExpressingMolecular(IHlaScoringMetadata metadata) =>
                metadata.TypingMethod == TypingMethod.Molecular && metadata.HlaScoringInfo.MatchingPGroups.Any();
            
            if (IsExpressingMolecular(patientMetadata) &&
                IsExpressingMolecular(donorMetadata) &&
                patientMetadata.HlaScoringInfo.MatchingPGroups.Intersect(donorMetadata.HlaScoringInfo.MatchingPGroups).Any())
            {
                return true;
            }

            // if both typings have assigned serologies, then determine if they are antigen-matched by checking for overlapping assignments
            return patientMatchingSerologies.Any() && donorSerologyEquivalents.Any() && patientMatchingSerologies.Intersect(donorSerologyEquivalents).Any();
        }
    }
}