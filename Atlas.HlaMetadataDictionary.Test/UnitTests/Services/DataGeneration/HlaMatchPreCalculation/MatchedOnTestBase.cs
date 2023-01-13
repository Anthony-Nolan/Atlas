using System.Collections.Generic;
using System.Linq;
using Atlas.Common.GeneticData;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Test.SharedTestHelpers;
using Atlas.HlaMetadataDictionary.InternalModels.MatchingTypings;
using NUnit.Framework;

namespace Atlas.HlaMetadataDictionary.Test.UnitTests.Services.DataGeneration.HlaMatchPreCalculation
{
    internal abstract class MatchedOnTestBase<TMatchedOn> where TMatchedOn : IMatchedOn
    {
        protected List<TMatchedOn> MatchedHla { get; set; }

        protected TMatchedOn GetSingleMatchingTyping(Locus locus, string hlaName)
        {
            return MatchedHla.Single(m => m.HlaTyping.Locus.Equals(locus) && m.HlaTyping.Name.Equals(hlaName));
        }

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage(() =>
            {
                var matchedHlaFromCache = SharedTestDataCache.GetMatchedHla();
                MatchedHla = matchedHlaFromCache.OfType<TMatchedOn>().ToList();
            });
        }
    }
}
