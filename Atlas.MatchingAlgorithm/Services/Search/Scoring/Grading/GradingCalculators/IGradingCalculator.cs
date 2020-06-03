using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata.ScoringMetadata;
using Atlas.MatchingAlgorithm.Client.Models.SearchResults.PerLocus;

namespace Atlas.MatchingAlgorithm.Services.Search.Scoring.Grading.GradingCalculators
{
    public interface IGradingCalculator
    {
        MatchGrade CalculateGrade(
            IHlaScoringMetadata patientMetadata,
            IHlaScoringMetadata donorMetadata);
    }
}
