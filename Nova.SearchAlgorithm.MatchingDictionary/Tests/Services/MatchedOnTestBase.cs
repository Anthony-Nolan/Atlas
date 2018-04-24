using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypes;
using System.Collections.Generic;
using System.Linq;

namespace Nova.SearchAlgorithm.MatchingDictionary.Tests.Services
{
    public class MatchedOnTestBase<TMatchedOn> where TMatchedOn : IMatchedOn
    {
        protected List<TMatchedOn> MatchingTypes;

        public MatchedOnTestBase(IEnumerable<TMatchedOn> matchingTypes)
        {
            MatchingTypes = matchingTypes.ToList();
        }

        protected TMatchedOn GetSingleMatchingType(string matchLocus, string hlaName)
        {
            return MatchingTypes.Single(m =>
                m.HlaType.MatchLocus.Equals(matchLocus) && m.HlaType.Name.Equals(hlaName));
        }
    }
}
