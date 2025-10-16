using Atlas.Client.Models.Search.Results.Matching.PerLocus;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata.ScoringMetadata;

namespace Atlas.MatchingAlgorithm.Services.Search.Scoring.Grading.GradingCalculators
{
    public interface INewAlleleGradingCalculator : IGradingCalculator
    {
    }

    public class NewAlleleGradingCalculator : GradingCalculatorBase, INewAlleleGradingCalculator
    {

        protected override bool ScoringInfosAreOfPermittedTypes(IHlaScoringInfo patientInfo, IHlaScoringInfo donorInfo)
        {
            return patientInfo is NewAlleleScoringInfo || donorInfo is NewAlleleScoringInfo;
        }

        protected override MatchGrade GetMatchGrade(IHlaScoringMetadata patientMetadata, IHlaScoringMetadata donorMetadata)
        {
            return MatchGrade.Mismatch;
        }
    }
}
