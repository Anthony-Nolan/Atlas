using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Models.Scoring;

namespace Nova.SearchAlgorithm.Services.Scoring
{
    public interface IGradingService
    {
        PhenotypeInfo<MatchGradeResult> CalculateGrades(MatchResultWithMatchingDictionaryEntries matches, MismatchCriteria criteria);
    }

    public class GradingService: IGradingService
    {
        public PhenotypeInfo<MatchGradeResult> CalculateGrades(MatchResultWithMatchingDictionaryEntries matches, MismatchCriteria criteria)
        {
            // TODO: NOVA-1446: Implement
            throw new System.NotImplementedException();
        }
    }
}