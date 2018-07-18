using FluentAssertions;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypings;
using Nova.SearchAlgorithm.MatchingDictionary.Services.HlaDataConversion;
using NSubstitute;
using NUnit.Framework;
using System.Collections.Generic;

namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Services.HlaDataConversion
{
    public abstract class MatchedHlaDataConverterTestBase<TGenerator>
        where TGenerator : IMatchedHlaDataConverterBase, new()
    {
        protected TGenerator LookupResultGenerator;
        protected const MatchLocus MatchedLocus = MatchLocus.A;
        protected const SerologySubtype SeroSubtype = SerologySubtype.Broad;
        protected const string SerologyName = "999";
        
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            LookupResultGenerator = new TGenerator();
        }

        [Test]
        public void ConvertToHlaLookupResults_WhenSerology_OneLookupResultGenerated()
        {
            var infoForMatching = Substitute.For<ISerologyInfoForMatching>();
            infoForMatching.HlaTyping.Returns(new SerologyTyping(MatchedLocus.ToString(), SerologyName, SeroSubtype));
            var matchedSerology = new MatchedSerology(infoForMatching, new List<string>(), new List<string>());

            var actualLookupResults = LookupResultGenerator.ConvertToHlaLookupResults(new[] { matchedSerology });

            var expectedLookupResults = new List<IHlaLookupResult>
            {
                BuildExpectedSerologyHlaLookupResult()
            };

            actualLookupResults.Should().BeEquivalentTo(expectedLookupResults);
        }

        protected abstract IHlaLookupResult BuildExpectedSerologyHlaLookupResult();

        protected static MatchedAllele BuildMatchedAllele(string alleleName)
        {
            var infoForMatching = Substitute.For<IAlleleInfoForMatching>();
            infoForMatching.HlaTyping.Returns(new AlleleTyping(MatchedLocus, alleleName));

            return new MatchedAllele(infoForMatching, new List<SerologyMappingForAllele>());
        }
    }
}
