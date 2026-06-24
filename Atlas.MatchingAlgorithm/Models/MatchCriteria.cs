using Atlas.MatchingAlgorithm.Common.Models;

namespace Atlas.MatchingAlgorithm.Models;

public class MatchCriteria
{
    public AlleleLevelMatchCriteria AlleleLevelMatchCriteria { set; get; }
    public NonHlaFilteringCriteria NonHlaFilteringCriteria { set; get; }
}