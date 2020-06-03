﻿using System.Linq;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata.ScoringMetadata;
using Atlas.MatchingAlgorithm.Client.Models.SearchResults.PerLocus;

namespace Atlas.MatchingAlgorithm.Services.Search.Scoring.Grading.GradingCalculators
{
    /// <summary>
    /// To be used when both typings are molecular, and at least
    /// one has consolidated molecular scoring info.
    /// </summary>
    public interface IConsolidatedMolecularGradingCalculator : IGradingCalculator
    {
    }

    public class ConsolidatedMolecularGradingCalculator :
        GradingCalculatorBase,
        IConsolidatedMolecularGradingCalculator
    {
        private readonly IPermissiveMismatchCalculator permissiveMismatchCalculator;

        public ConsolidatedMolecularGradingCalculator(IPermissiveMismatchCalculator permissiveMismatchCalculator)
        {
            this.permissiveMismatchCalculator = permissiveMismatchCalculator;
        }

        protected override bool ScoringInfosAreOfPermittedTypes(
            IHlaScoringInfo patientInfo,
            IHlaScoringInfo donorInfo)
        {
            return (patientInfo is ConsolidatedMolecularScoringInfo ||
                    donorInfo is ConsolidatedMolecularScoringInfo) &&
                   !(patientInfo is SerologyScoringInfo) &&
                   !(donorInfo is SerologyScoringInfo);
        }

        protected override MatchGrade GetMatchGrade(
            IHlaScoringMetadata patientMetadata, 
            IHlaScoringMetadata donorMetadata)
        {
            var patientInfo = patientMetadata.HlaScoringInfo;
            var donorInfo = donorMetadata.HlaScoringInfo;

            // Order of the following checks is critical to the grade outcome

            if (patientInfo.Equals(donorInfo))
            {
                return MatchGrade.GGroup;
            }
            else if (IsGGroupMatch(patientInfo, donorInfo))
            {
                return MatchGrade.GGroup;
            }
            else if (IsPGroupMatch(patientInfo, donorInfo))
            {
                return MatchGrade.PGroup;
            }
            else if (IsPermissiveMismatch(patientMetadata, donorMetadata))
            {
                return MatchGrade.PermissiveMismatch;
            }

            return MatchGrade.Mismatch;
        }

        /// <summary>
        /// Do both typings have intersecting G Groups?
        /// </summary>
        private static bool IsGGroupMatch(
            IHlaScoringInfo patientInfo,
            IHlaScoringInfo donorInfo)
        {
            return patientInfo.MatchingGGroups
                .Intersect(donorInfo.MatchingGGroups)
                .Any();
        }

        /// <summary>
        /// Do both typings have intersecting P Groups?
        /// </summary>
        private static bool IsPGroupMatch(
            IHlaScoringInfo patientInfo,
            IHlaScoringInfo donorInfo)
        {
            return patientInfo.MatchingPGroups
                .Intersect(donorInfo.MatchingPGroups)
                .Any();
        }

        private bool IsPermissiveMismatch(
            IHlaMetadata patientMetadata,
            IHlaMetadata donorMetadata)
        {
            return permissiveMismatchCalculator.IsPermissiveMismatch(
                patientMetadata.Locus,
                patientMetadata.LookupName,
                donorMetadata.LookupName);
        }
    }
}