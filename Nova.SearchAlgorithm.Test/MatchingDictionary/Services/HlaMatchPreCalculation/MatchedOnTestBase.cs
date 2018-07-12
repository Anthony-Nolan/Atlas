using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypings;

namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Services.HlaMatchPreCalculation
{
    public class MatchedOnTestBase<TMatchedOn> where TMatchedOn : IMatchedOn
    {
        protected List<TMatchedOn> MatchedHlaTypings;

        public MatchedOnTestBase(IEnumerable<TMatchedOn> matchingTypes)
        {
            MatchedHlaTypings = matchingTypes.ToList();
        }

        protected TMatchedOn GetSingleMatchingTyping(MatchLocus matchLocus, string hlaName)
        {
            return MatchedHlaTypings.Single(m =>
                m.HlaTyping.MatchLocus.Equals(matchLocus) && m.HlaTyping.Name.Equals(hlaName));
        }
    }
}
