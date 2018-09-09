using System;
using System.Threading.Tasks;
using FluentAssertions;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.MatchingLookup;
using Nova.SearchAlgorithm.MatchingDictionary.Services;
using NSubstitute;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Services.Lookups
{
    [TestFixture]
    public class LocusHlaMatchingLookupServiceTest
    {
        private const MatchLocus MatchedLocus = MatchLocus.A;
        private IHlaMatchingLookupService matchingLookupService;
        private ILocusHlaMatchingLookupService locusHlaMatchingLookupService;

        [SetUp]
        public void LocusHlaMatchingLookupServiceTest_SetUpBeforeEachTest()
        {
            matchingLookupService = Substitute.For<IHlaMatchingLookupService>();
            locusHlaMatchingLookupService = new LocusHlaMatchingLookupService(matchingLookupService);
        }

        [TestCase(TypingMethod.Molecular, TypingMethod.Molecular)]
        [TestCase(TypingMethod.Serology, TypingMethod.Serology)]
        [TestCase(TypingMethod.Molecular, TypingMethod.Serology)]
        [TestCase(TypingMethod.Serology, TypingMethod.Molecular)]
        public async Task GetHlaMatchingLookupResultForLocus_WhenTwoExpressingTypings_ReturnsExpectedLookupResults(
            TypingMethod typingMethod1,
            TypingMethod typingMethod2)
        {
            const string hlaString1 = "hla-1";
            const string hlaString2 = "hla-2";
            const string pGroup1 = "p-group-1";
            const string pGroup2 = "p-group-2";

            var lookupResult1 =
                new HlaMatchingLookupResult(MatchedLocus, hlaString1, typingMethod1, new[] { pGroup1 });
            var lookupResult2 =
                new HlaMatchingLookupResult(MatchedLocus, hlaString2, typingMethod2, new[] { pGroup2 });

            matchingLookupService
                .GetHlaLookupResult(MatchedLocus, Arg.Any<string>())
                .Returns(lookupResult1, lookupResult2);

            var expectedResults =
                new Tuple<IHlaMatchingLookupResult, IHlaMatchingLookupResult>(
                    lookupResult1,
                    lookupResult2);

            var actualResults = await locusHlaMatchingLookupService.GetHlaMatchingLookupResultForLocus(
                    MatchedLocus,
                    new Tuple<string, string>(hlaString1, hlaString2));

            actualResults.ShouldBeEquivalentTo(expectedResults);
        }
    }
}
