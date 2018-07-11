using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Common.Models.Scoring;

namespace Nova.SearchAlgorithm.Services.Scoring
{
    public interface IGradingService
    {
        MatchGradeResult CalculateGrade(MatchResultWithMatchingDictionaryEntries matches, MismatchCriteria criteria);
    }

    public class GradingService: IGradingService
    {
        public MatchGradeResult CalculateGrade(MatchResultWithMatchingDictionaryEntries matches, MismatchCriteria criteria)
        {
            // TODO: NOVA-1446: Implement
            throw new System.NotImplementedException();
        }
    }
}