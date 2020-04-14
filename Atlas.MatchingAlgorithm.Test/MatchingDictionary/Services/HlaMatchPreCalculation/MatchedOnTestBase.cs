using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.MatchingDictionary.Models.MatchingTypings;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace Atlas.MatchingAlgorithm.Test.MatchingDictionary.Services.HlaMatchPreCalculation
{
    public abstract class MatchedOnTestBase<TMatchedOn> where TMatchedOn : IMatchedOn
    {
        protected const string HlaDatabaseVersionToTest = "3330";

        protected List<TMatchedOn> MatchedHla { get; set; }

        protected TMatchedOn GetSingleMatchingTyping(Locus locus, string hlaName)
        {
            return MatchedHla.Single(m => m.HlaTyping.Locus.Equals(locus) && m.HlaTyping.Name.Equals(hlaName));
        }

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            var matchedHlaFromCache = SharedTestDataCache.GetMatchedHla();
            MatchedHla = matchedHlaFromCache.OfType<TMatchedOn>().ToList();
        }
    }
}
