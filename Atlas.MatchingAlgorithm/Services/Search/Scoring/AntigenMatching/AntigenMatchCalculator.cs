using System;
using System.Linq;
using Atlas.Client.Models.Search.Results.Matching.PerLocus;
using Atlas.Common.Utils.Extensions;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata.ScoringMetadata;

namespace Atlas.MatchingAlgorithm.Services.Search.Scoring.AntigenMatching
{
    internal interface IAntigenMatchCalculator
    {
        /// <summary>
        /// Are <paramref name="patientMetadata"/> and <paramref name="donorMetadata"/> antigen matched?
        /// <inheritdoc cref="LocusPositionScoreDetails.IsAntigenMatch"/>
        /// </summary>
        /// <returns></returns>
        bool? IsAntigenMatch(MatchGrade? matchGrade, IHlaScoringMetadata patientMetadata, IHlaScoringMetadata donorMetadata);
    }

    internal class AntigenMatchCalculator : IAntigenMatchCalculator
    {
        /// <inheritdoc />
        public bool? IsAntigenMatch(MatchGrade? matchGrade, IHlaScoringMetadata patientMetadata, IHlaScoringMetadata donorMetadata)
        {
            const string nullMatchGradePrefix = "Null";

            if (matchGrade is null or MatchGrade.Unknown || patientMetadata is null || donorMetadata is null)
            {
                return null;
            }

            if (patientMetadata.Locus != donorMetadata.Locus)
            {
                throw new ArgumentException($"Cannot calculate antigen match as patient typing is from locus {patientMetadata.Locus} and donor typing is from locus {donorMetadata.Locus}.");
            }

            if (matchGrade.ToString()!.StartsWith(nullMatchGradePrefix))
            {
                return false;
            }

            if (matchGrade == MatchGrade.Mismatch && PatientAndDonorAreNotAntigenMatched(patientMetadata, donorMetadata))
            {
                return false;
            }

            return true;
        }

        private static bool PatientAndDonorAreNotAntigenMatched(IHlaScoringMetadata patientMetadata, IHlaScoringMetadata donorMetadata)
        {
            var patientMatchingSerologies = patientMetadata.HlaScoringInfo.MatchingSerologies.Select(s => s.Name);
            var donorSerologyEquivalents = donorMetadata.HlaScoringInfo.MatchingSerologies.Where(s => s.IsDirectMapping).Select(s => s.Name);

            return patientMatchingSerologies.Intersect(donorSerologyEquivalents).IsNullOrEmpty();
        }
    }
}
