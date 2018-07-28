using Nova.SearchAlgorithm.Common.Models.Scoring;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.ScoringLookup;
using System.Linq;

namespace Nova.SearchAlgorithm.Services.Scoring.Grading
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
            IHlaScoringLookupResult patientLookupResult, 
            IHlaScoringLookupResult donorLookupResult)
        {
            var patientInfo = patientLookupResult.HlaScoringInfo;
            var donorInfo = donorLookupResult.HlaScoringInfo;

            // Order of the following checks is critical to the grade outcome

            if (patientInfo.Equals(donorInfo))
            {
                return MatchGrade.GGroup;
            }

            if (IsGGroupMatch(patientInfo, donorInfo))
            {
                return MatchGrade.GGroup;
            }

            if (IsPGroupMatch(patientInfo, donorInfo))
            {
                return MatchGrade.PGroup;
            }

            return MatchGrade.Mismatch;
        }

        /// <summary>
        /// Do both typings have interseting G Groups?
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
        /// Do both typings have interseting P Groups?
        /// </summary>
        private static bool IsPGroupMatch(
            IHlaScoringInfo patientInfo,
            IHlaScoringInfo donorInfo)
        {
            return patientInfo.MatchingPGroups
                .Intersect(donorInfo.MatchingPGroups)
                .Any();
        }
    }
}