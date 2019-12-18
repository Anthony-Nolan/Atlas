using FluentAssertions;
using Nova.HLAService.Client.Models;
using Nova.SearchAlgorithm.Common.Models;
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
    public class Dpb1TceGroupLookupServiceTest :
        HlaSearchingLookupServiceTestBase<IDpb1TceGroupsLookupRepository, IDpb1TceGroupLookupService, IDpb1TceGroupsLookupResult>
    {
        private const string ExpectedNoTceGroupAssignment = "";

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            MolecularLocus = MolecularLocusType.Dpb1;
            MatchedLocus = Locus.Dpb1;
        }

        [SetUp]
        public void Dpb1TceGroupsLookupServiceTest_SetUpBeforeEachTest()
        {
            LookupService = new Dpb1TceGroupLookupService(
                HlaLookupRepository,
                AlleleNamesLookupService,
                HlaCategorisationService,
                AlleleStringSplitterService,
                Cache);
        }

        [TestCase(HlaTypingCategory.AlleleStringOfSubtypes)]
        [TestCase(HlaTypingCategory.AlleleStringOfNames)]
        public async Task GetDpb1TceGroup_WhenAlleleStringMapsToSingleTceGroup_SingleTceGroupReturned(
            HlaTypingCategory category)
        {
            const string expectedLookupName = "Allele1/Allele2";
            const string sharedTceGroup = "shared-tce-group";
            const string firstTestAllele = "first";
            const string secondTestAllele = "second";

            HlaCategorisationService
                .GetHlaTypingCategory(expectedLookupName)
                .Returns(category);

            var expectedAlleleNames = new List<string>
            {
                firstTestAllele,
                secondTestAllele
            };
            AlleleStringSplitterService.GetAlleleNamesFromAlleleString(expectedLookupName)
                .Returns(expectedAlleleNames);

            var firstEntry = BuildTableEntityForSingleAllele(firstTestAllele, sharedTceGroup);
            var secondEntry = BuildTableEntityForSingleAllele(secondTestAllele, sharedTceGroup);

            HlaLookupRepository
                .GetHlaLookupTableEntityIfExists(MatchedLocus, Arg.Any<string>(), TypingMethod.Molecular, Arg.Any<string>())
                .Returns(firstEntry, secondEntry);

            var actualResult = await LookupService.GetDpb1TceGroup(expectedLookupName, "hla-db-version");

            actualResult.Should().Be(sharedTceGroup);
        }

        [TestCase(HlaTypingCategory.AlleleStringOfSubtypes)]
        [TestCase(HlaTypingCategory.AlleleStringOfNames)]
        public async Task GetDpb1TceGroup_WhenAlleleStringMapsToMoreThanOneTceGroup_NoTceGroupAssignmentReturned(
            HlaTypingCategory category)
        {
            const string expectedLookupName = "Allele1/Allele2";
            const string firstTestAllele = "first";
            const string secondTestAllele = "second";

            HlaCategorisationService
                .GetHlaTypingCategory(expectedLookupName)
                .Returns(category);

            var expectedAlleleNames = new List<string>
            {
                firstTestAllele,
                secondTestAllele
            };
            AlleleStringSplitterService.GetAlleleNamesFromAlleleString(expectedLookupName)
                .Returns(expectedAlleleNames);

            var firstEntry = BuildTableEntityForSingleAllele(firstTestAllele);
            var secondEntry = BuildTableEntityForSingleAllele(secondTestAllele);

            HlaLookupRepository
                .GetHlaLookupTableEntityIfExists(MatchedLocus, Arg.Any<string>(), TypingMethod.Molecular, Arg.Any<string>())
                .Returns(firstEntry, secondEntry);

            var actualResult = await LookupService.GetDpb1TceGroup(expectedLookupName, "hla-db-version");

            actualResult.Should().Be(ExpectedNoTceGroupAssignment);
        }

        protected override HlaLookupTableEntity BuildTableEntityForSingleAllele(string alleleName)
        {
            var lookupResult = new Dpb1TceGroupsLookupResult(alleleName, alleleName);
            return new HlaLookupTableEntity(lookupResult);
        }

        private static HlaLookupTableEntity BuildTableEntityForSingleAllele(string alleleName, string tceGroup)
        {
            var lookupResult = new Dpb1TceGroupsLookupResult(alleleName, tceGroup);
            return new HlaLookupTableEntity(lookupResult);
        }
    }
}