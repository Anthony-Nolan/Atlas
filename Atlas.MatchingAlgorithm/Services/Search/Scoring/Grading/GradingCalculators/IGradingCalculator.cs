using Atlas.Client.Models.Search.Results.Matching.PerLocus;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata.ScoringMetadata;

namespace Atlas.MatchingAlgorithm.Services.Search.Scoring.Grading.GradingCalculators
{
    public interface IGradingCalculator
    {
        MatchGrade CalculateGrade(
            IHlaScoringMetadata patientMetadata,
            IHlaScoringMetadata donorMetadata);
    }
}
