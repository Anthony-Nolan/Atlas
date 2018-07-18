using FluentAssertions;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;
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
            var serologyTyping = new SerologyTyping(MatchedLocus.ToString(), SerologyName, SeroSubtype);

            var infoForMatching = Substitute.For<ISerologyInfoForMatching>();
            infoForMatching.HlaTyping.Returns(serologyTyping);
            infoForMatching.MatchingSerologies.Returns(new[] {serologyTyping});

            var matchedSerology = new MatchedSerology(infoForMatching, new List<string>(), new List<string>());

            var actualLookupResults = LookupResultGenerator.ConvertToHlaLookupResults(new[] { matchedSerology });

            var expectedLookupResults = new List<IHlaLookupResult>
            {
                BuildExpectedSerologyHlaLookupResult()
            };

            actualLookupResults.Should().BeEquivalentTo(expectedLookupResults);
        }

        protected abstract IHlaLookupResult BuildExpectedSerologyHlaLookupResult();

        /// <summary>
        /// Builds a Matched Allele.
        /// Submitted allele name is used for the Name, and the Matching P/G groups.
        /// Every allele will have the same Serology mapping, built from constant values.
        /// </summary>
        protected static MatchedAllele BuildMatchedAllele(string alleleName)
        {
            var infoForMatching = Substitute.For<IAlleleInfoForMatching>();
            infoForMatching.HlaTyping.Returns(new AlleleTyping(MatchedLocus, alleleName));
            infoForMatching.MatchingPGroups.Returns(new[] { alleleName });
            infoForMatching.MatchingGGroups.Returns(new[] { alleleName });

            var serologyTyping = new SerologyTyping(MatchedLocus.ToString(), SerologyName, SeroSubtype);
            var serologyMatch = new SerologyMatch(serologyTyping);
            var mapping = new SerologyMappingForAllele(serologyTyping, Assignment.None, new[] { serologyMatch });

            return new MatchedAllele(infoForMatching, new[] { mapping });
        }
    }
}
