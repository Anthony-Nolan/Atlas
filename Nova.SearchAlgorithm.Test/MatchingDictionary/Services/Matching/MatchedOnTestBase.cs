using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypings;

namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Services.Matching
{
    public class MatchedOnTestBase<TMatchedOn> where TMatchedOn : IMatchedOn
    {
        protected List<TMatchedOn> MatchingTypings;

        public MatchedOnTestBase(IEnumerable<TMatchedOn> matchingTypes)
        {
            MatchingTypings = matchingTypes.ToList();
        }

        protected TMatchedOn GetSingleMatchingTyping(MatchLocus matchLocus, string hlaName)
        {
            return MatchingTypings.Single(m =>
                m.HlaTyping.MatchLocus.Equals(matchLocus) && m.HlaTyping.Name.Equals(hlaName));
        }
    }
}
