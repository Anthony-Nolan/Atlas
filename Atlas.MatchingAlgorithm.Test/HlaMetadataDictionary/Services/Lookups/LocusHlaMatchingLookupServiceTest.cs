using FluentAssertions;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.HlaMetadataDictionary.Models.HLATypings;
using Atlas.HlaMetadataDictionary.Models.Lookups.MatchingLookup;
using Atlas.HlaMetadataDictionary.Services;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;

namespace Atlas.MatchingAlgorithm.Test.HlaMetadataDictionary.Services.Lookups
{
    [TestFixture]
    public class LocusHlaMatchingLookupServiceTest
    {
        private const Locus MatchedLocus = Locus.A;
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
                new HlaMatchingLookupResult(MatchedLocus, hlaString1, typingMethod1, new[] {pGroup1});
            var lookupResult2 =
                new HlaMatchingLookupResult(MatchedLocus, hlaString2, typingMethod2, new[] {pGroup2});

            matchingLookupService
                .GetHlaLookupResult(MatchedLocus, Arg.Any<string>(), Arg.Any<string>())
                .Returns(lookupResult1, lookupResult2);

            var expectedResults =
                new Tuple<IHlaMatchingLookupResult, IHlaMatchingLookupResult>(
                    lookupResult1,
                    lookupResult2);

            var actualResults = await locusHlaMatchingLookupService.GetHlaMatchingLookupResults(
                MatchedLocus,
                new Tuple<string, string>(hlaString1, hlaString2), "hla-db-version");

            actualResults.ShouldBeEquivalentTo(expectedResults);
        }

        [TestCase(TypingMethod.Molecular)]
        [TestCase(TypingMethod.Serology)]
        public async Task GetHlaMatchingLookupResultForLocus_WhenPositionOneIsExpressedTyping_AndPositionTwoIsNullAllele_PGroupOfOneCopiedToTwo(
            TypingMethod expressedHlaTypingMethod)
        {
            const string typingInPosition1 = "expressed-hla";
            const string typingInPosition2 = "null-allele";
            const string pGroup = "expressed-hla-p-group";

            var expressedHlaResult =
                new HlaMatchingLookupResult(MatchedLocus, typingInPosition1, expressedHlaTypingMethod, new[] {pGroup});
            var nullAlleleResult =
                new HlaMatchingLookupResult(MatchedLocus, typingInPosition2, TypingMethod.Molecular, new string[] { });

            matchingLookupService
                .GetHlaLookupResult(MatchedLocus, Arg.Any<string>(), Arg.Any<string>())
                .Returns(expressedHlaResult, nullAlleleResult);

            var nullAlleleResultWithExpressedPGroup =
                new HlaMatchingLookupResult(MatchedLocus, typingInPosition2, TypingMethod.Molecular, new[] {pGroup});

            var expectedResults =
                new Tuple<IHlaMatchingLookupResult, IHlaMatchingLookupResult>(
                    expressedHlaResult,
                    nullAlleleResultWithExpressedPGroup);

            var actualResults = await locusHlaMatchingLookupService.GetHlaMatchingLookupResults(
                MatchedLocus,
                new Tuple<string, string>(typingInPosition1, typingInPosition2), "hla-db-version");

            actualResults.ShouldBeEquivalentTo(expectedResults);
        }

        [TestCase(TypingMethod.Molecular)]
        [TestCase(TypingMethod.Serology)]
        public async Task GetHlaMatchingLookupResultForLocus_WhenPositionOneIsNullAllele_AndPositionTwoIsExpressedTyping_PGroupOfTwoCopiedToOne(
            TypingMethod expressedHlaTypingMethod)
        {
            const string typingInPosition1 = "null-allele";
            const string typingInPosition2 = "expressed-hla";
            const string pGroup = "expressed-hla-p-group";

            var nullAlleleResult =
                new HlaMatchingLookupResult(MatchedLocus, typingInPosition1, TypingMethod.Molecular, new string[] { });
            var expressedHlaResult =
                new HlaMatchingLookupResult(MatchedLocus, typingInPosition2, expressedHlaTypingMethod, new[] {pGroup});

            matchingLookupService
                .GetHlaLookupResult(MatchedLocus, Arg.Any<string>(), Arg.Any<string>())
                .Returns(nullAlleleResult, expressedHlaResult);

            var nullAlleleResultWithExpressedPGroup =
                new HlaMatchingLookupResult(MatchedLocus, typingInPosition1, TypingMethod.Molecular, new[] {pGroup});

            var expectedResults =
                new Tuple<IHlaMatchingLookupResult, IHlaMatchingLookupResult>(
                    nullAlleleResultWithExpressedPGroup,
                    expressedHlaResult);

            var actualResults = await locusHlaMatchingLookupService.GetHlaMatchingLookupResults(
                MatchedLocus,
                new Tuple<string, string>(typingInPosition1, typingInPosition2), "hla-db-version");

            actualResults.ShouldBeEquivalentTo(expectedResults);
        }
    }
}