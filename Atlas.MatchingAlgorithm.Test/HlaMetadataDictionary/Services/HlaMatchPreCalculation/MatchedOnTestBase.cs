using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.HlaMetadataDictionary.Models.MatchingTypings;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using Atlas.Common.GeneticData;

namespace Atlas.MatchingAlgorithm.Test.HlaMetadataDictionary.Services.HlaMatchPreCalculation
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
