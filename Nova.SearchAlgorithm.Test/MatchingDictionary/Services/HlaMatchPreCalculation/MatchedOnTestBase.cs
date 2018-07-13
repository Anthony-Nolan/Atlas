using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypings;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Services.HlaMatchPreCalculation
{
    public abstract class MatchedOnTestBase<TMatchedOn> where TMatchedOn : IMatchedOn
    {
        protected List<TMatchedOn> MatchedHla { get; set; }

        protected TMatchedOn GetSingleMatchingTyping(MatchLocus matchLocus, string hlaName)
        {
            return MatchedHla.Single(m => m.HlaTyping.MatchLocus.Equals(matchLocus) && m.HlaTyping.Name.Equals(hlaName));
        }

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            var matchedHlaFromCache = SharedTestDataCache.GetMatchedHla();
            MatchedHla = matchedHlaFromCache.OfType<TMatchedOn>().ToList();
        }
    }
}
