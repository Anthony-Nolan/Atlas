using Atlas.Client.Models.Search.Results.Matching.PerLocus;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata.ScoringMetadata;

namespace Atlas.MatchingAlgorithm.Services.Search.Scoring.Grading.GradingCalculators
{
    /// <summary>
    /// Calculates match grades when one allele is expressing and the other is null-expressing.
    /// This can be used both for single allele comparisons: e.g. 01:01N vs 01:02
    /// Or for comparison of a single null vs multiple expressing alleles: e.g. 01:01N vs 01:02/01:03 or 01:NMDP
    /// </summary>
    public class ExpressingVsNullAlleleGradingCalculator : GradingCalculatorBase
    {
        protected override bool ScoringInfosAreOfPermittedTypes(IHlaScoringInfo patientInfo, IHlaScoringInfo donorInfo)
        {
            return patientInfo is SingleAlleleScoringInfo || donorInfo is SingleAlleleScoringInfo;
        }

        protected override MatchGrade GetMatchGrade(IHlaScoringMetadata patientMetadata, IHlaScoringMetadata donorMetadata)
        {
            return MatchGrade.Mismatch;
        }
    }
}