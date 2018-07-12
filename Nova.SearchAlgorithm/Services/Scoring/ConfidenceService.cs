using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Models.Scoring;
using Nova.SearchAlgorithm.MatchingDictionary.Models;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingDictionary;

namespace Nova.SearchAlgorithm.Services.Scoring
{
    public interface IConfidenceService
    {
        PhenotypeInfo<MatchConfidence> CalculateMatchConfidences(MatchResultWithPreCalculatedHlaMatchInfo matches, MismatchCriteria criteria, PhenotypeInfo<MatchGradeResult> matchGrades);
    }

    public class ConfidenceService: IConfidenceService
    {
        public PhenotypeInfo<MatchConfidence> CalculateMatchConfidences(MatchResultWithPreCalculatedHlaMatchInfo matches, MismatchCriteria criteria, PhenotypeInfo<MatchGradeResult> matchGrades)
        {
            // TODO: NOVA-1447: Implement
            throw new System.NotImplementedException();
        }
    }
}