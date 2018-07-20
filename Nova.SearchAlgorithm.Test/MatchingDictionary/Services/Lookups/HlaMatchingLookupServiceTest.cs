using Microsoft.Extensions.Caching.Memory;
using Nova.HLAService.Client.Models;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.MatchingLookup;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage;
using Nova.SearchAlgorithm.MatchingDictionary.Services;
using Nova.Utils.ApplicationInsights;
using NSubstitute;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;

namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Services.Lookups
{
    [TestFixture]
    public class HlaMatchingLookupServiceTest : 
        HlaSearchingLookupServiceTestBase<IHlaMatchingLookupRepository, IHlaMatchingLookupService, IHlaMatchingLookupResult>
    {
        [OneTimeSetUp]
        public void HlaMatchingLookupServiceTest_OneTimeSetUp()
        {
            var memoryCache = Substitute.For<IMemoryCache>();
            var logger = Substitute.For<ILogger>();

            LookupService = new HlaMatchingLookupService(
                HlaLookupRepository,
                AlleleNamesLookupService,
                HlaServiceClient,
                HlaCategorisationService,
                AlleleStringSplitterService,
                memoryCache,
                logger);
        }

        [Test]
        public async Task GetHlaLookupResult_WhenNmdpCode_MatchingHlaForAllAllelesIsReturned()
        {
            const string expectedLookupName = "99:NMDPCODE";
            const string firstAlleleName = "99:01";
            const string secondAlleleName = "99:50";
            const string thirdAlleleName = "99:99";

            HlaCategorisationService
                .GetHlaTypingCategory(expectedLookupName)
                .Returns(HlaTypingCategory.NmdpCode);
          
            HlaServiceClient
                .GetAllelesForDefinedNmdpCode(MolecularLocus, expectedLookupName)
                .Returns(new List<string> { firstAlleleName, secondAlleleName, thirdAlleleName });

            var firstEntry = BuildTableEntityForSingleAllele(firstAlleleName);
            var secondEntry = BuildTableEntityForSingleAllele(secondAlleleName);
            var thirdEntry = BuildTableEntityForSingleAllele(thirdAlleleName);

            HlaLookupRepository
                .GetHlaLookupTableEntityIfExists(MatchedLocus, Arg.Any<string>(), TypingMethod.Molecular)
                .Returns(firstEntry, secondEntry, thirdEntry);

            var actualResult = await LookupService.GetHlaLookupResult(MatchedLocus, expectedLookupName);
            var expectedMatchingPGroups = new[] { firstAlleleName, secondAlleleName, thirdAlleleName };

            actualResult.MatchingPGroups.ShouldBeEquivalentTo(expectedMatchingPGroups);
        }

        [TestCase(HlaTypingCategory.AlleleStringOfSubtypes, "Family:Subtype1/Subtype2", "Family:Subtype1", "Family:Subtype2")]
        [TestCase(HlaTypingCategory.AlleleStringOfNames, "Allele1/Allele2", "Allele1", "Allele2")]
        public async Task GetHlaLookupResult_WhenAlleleString_MatchingHlaForAllAllelesIsReturned(
            HlaTypingCategory category, string expectedLookupName, string firstAlleleName, string secondAlleleName)
        {
            HlaCategorisationService
                .GetHlaTypingCategory(expectedLookupName)
                .Returns(category);

            var expectedAlleleNames = new List<string> { firstAlleleName, secondAlleleName };
            AlleleStringSplitterService.GetAlleleNamesFromAlleleString(expectedLookupName)
                .Returns(expectedAlleleNames);

            var firstEntry = BuildTableEntityForSingleAllele(firstAlleleName);
            var secondEntry = BuildTableEntityForSingleAllele(secondAlleleName);

            HlaLookupRepository
                .GetHlaLookupTableEntityIfExists(MatchedLocus, Arg.Any<string>(), TypingMethod.Molecular)
                .Returns(firstEntry, secondEntry);

            var actualResult = await LookupService.GetHlaLookupResult(MatchedLocus, expectedLookupName);
            var expectedMatchingPGroups = expectedAlleleNames;

            actualResult.MatchingPGroups.ShouldBeEquivalentTo(expectedMatchingPGroups);
        }

        protected override HlaLookupTableEntity BuildTableEntityForSingleAllele(string alleleName)
        {
            var lookupResult = new HlaMatchingLookupResult(
                MatchedLocus,
                alleleName,
                TypingMethod.Molecular,
                new List<string> { alleleName }
                );

            return new HlaLookupTableEntity(lookupResult, lookupResult.MatchingPGroups);
        }
    }
}
