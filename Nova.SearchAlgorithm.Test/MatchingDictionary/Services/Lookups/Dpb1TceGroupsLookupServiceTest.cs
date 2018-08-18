using FluentAssertions;
using Nova.HLAService.Client.Models;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.Dpb1TceGroupLookup;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage;
using Nova.SearchAlgorithm.MatchingDictionary.Services;
using NSubstitute;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Services.Lookups
{
    [TestFixture]
    public class Dpb1TceGroupsLookupServiceTest :
        HlaSearchingLookupServiceTestBase<IDpb1TceGroupsLookupRepository, IDpb1TceGroupsLookupService, IDpb1TceGroupsLookupResult>
    {
        private static readonly string[,] Dpb1Alleles = { { "99:01", "1" }, { "99:50", "2" }, { "99:99", "3" } };

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            MolecularLocus = MolecularLocusType.Dpb1;
            MatchedLocus = MatchLocus.Dpb1;
        }

        [SetUp]
        public void Dpb1TceGroupsLookupServiceTest_SetUpBeforeEachTest()
        {
            LookupService = new Dpb1TceGroupsLookupService(
                HlaLookupRepository,
                AlleleNamesLookupService,
                HlaServiceClient,
                HlaCategorisationService,
                AlleleStringSplitterService,
                MemoryCache,
                Logger);
        }

        [Test]
        public async Task GetHlaLookupResult_WhenNmdpCode_TceGroupsForAllAllelesIsReturned()
        {
            const string expectedLookupName = "99:NMDPCODE";
            var firstAlleleName = Dpb1Alleles[0, 0];
            var secondAlleleName = Dpb1Alleles[1, 0];
            var thirdAlleleName = Dpb1Alleles[2, 0];

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
            var expectedTceGroups = new[] { Dpb1Alleles[0, 1], Dpb1Alleles[1, 1], Dpb1Alleles[2, 1] };

            actualResult.TceGroups.ShouldBeEquivalentTo(expectedTceGroups);
        }

        [TestCase(HlaTypingCategory.AlleleStringOfSubtypes)]
        [TestCase(HlaTypingCategory.AlleleStringOfNames)]
        public async Task GetHlaLookupResult_WhenAlleleString_TceGroupsForAllAllelesIsReturned(
            HlaTypingCategory category)
        {
            const string expectedLookupName = "Allele1/Allele2";
            var firstAlleleName = Dpb1Alleles[0, 0];
            var secondAlleleName = Dpb1Alleles[1, 0];

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
            var expectedTceGroups = new[] { Dpb1Alleles[0, 1], Dpb1Alleles[1, 1] };

            actualResult.TceGroups.ShouldBeEquivalentTo(expectedTceGroups);
        }

        protected override HlaLookupTableEntity BuildTableEntityForSingleAllele(string alleleName)
        {
            var lookupResult = new Dpb1TceGroupsLookupResult(
                alleleName,
                new[] { GetTceGroupFromTestDataset(alleleName) }
                );

            return new HlaLookupTableEntity(lookupResult);
        }

        private static string GetTceGroupFromTestDataset(string alleleName)
        {
            for (var i = 0; i < Dpb1Alleles.Length/2; i++)
            {
                if (alleleName.Equals(Dpb1Alleles[i, 0]))
                {
                    return Dpb1Alleles[i, 1];
                }
            }
            return string.Empty;
        }
    }
}

