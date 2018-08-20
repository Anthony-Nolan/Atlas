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
using System.Linq;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Services.Lookups
{
    [TestFixture]
    public class Dpb1TceGroupsLookupServiceTest :
        HlaSearchingLookupServiceTestBase<IDpb1TceGroupsLookupRepository, IDpb1TceGroupsLookupService, IDpb1TceGroupsLookupResult>
    {
        private class Dpb1TestData
        {
            public string AlleleName { get; }
            public string TceGroup { get; }

            public Dpb1TestData(string alleleName, string tceGroup)
            {
                AlleleName = alleleName;
                TceGroup = tceGroup;
            }
        }

        private static readonly Dpb1TestData FirstTestAllele = new Dpb1TestData("99:01", "1");
        private static readonly Dpb1TestData SecondTestAllele = new Dpb1TestData("99:50", "2");
        private static readonly Dpb1TestData ThirdTestAllele = new Dpb1TestData("99:99", "3");
        private static readonly Dpb1TestData[] TestAlleles = new Dpb1TestData[]
        {
            FirstTestAllele,
            SecondTestAllele,
            ThirdTestAllele
        };

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

            HlaCategorisationService
                .GetHlaTypingCategory(expectedLookupName)
                .Returns(HlaTypingCategory.NmdpCode);

            HlaServiceClient
                .GetAllelesForDefinedNmdpCode(MolecularLocus, expectedLookupName)
                .Returns(new List<string>
                {
                    FirstTestAllele.AlleleName,
                    SecondTestAllele.AlleleName,
                    ThirdTestAllele.AlleleName
                });

            var firstEntry = BuildTableEntityForSingleAllele(FirstTestAllele.AlleleName);
            var secondEntry = BuildTableEntityForSingleAllele(SecondTestAllele.AlleleName);
            var thirdEntry = BuildTableEntityForSingleAllele(ThirdTestAllele.AlleleName);

            HlaLookupRepository
                .GetHlaLookupTableEntityIfExists(MatchedLocus, Arg.Any<string>(), TypingMethod.Molecular)
                .Returns(firstEntry, secondEntry, thirdEntry);

            var actualResult = await LookupService.GetHlaLookupResult(MatchedLocus, expectedLookupName);
            var expectedTceGroups = new[]
            {
                FirstTestAllele.TceGroup,
                SecondTestAllele.TceGroup,
                ThirdTestAllele.TceGroup
            };

            actualResult.TceGroups.ShouldBeEquivalentTo(expectedTceGroups);
        }

        [TestCase(HlaTypingCategory.AlleleStringOfSubtypes)]
        [TestCase(HlaTypingCategory.AlleleStringOfNames)]
        public async Task GetHlaLookupResult_WhenAlleleString_TceGroupsForAllAllelesIsReturned(
            HlaTypingCategory category)
        {
            const string expectedLookupName = "Allele1/Allele2";

            HlaCategorisationService
                .GetHlaTypingCategory(expectedLookupName)
                .Returns(category);

            var expectedAlleleNames = new List<string>
            {
                FirstTestAllele.AlleleName,
                SecondTestAllele.AlleleName
            };
            AlleleStringSplitterService.GetAlleleNamesFromAlleleString(expectedLookupName)
                .Returns(expectedAlleleNames);

            var firstEntry = BuildTableEntityForSingleAllele(FirstTestAllele.AlleleName);
            var secondEntry = BuildTableEntityForSingleAllele(SecondTestAllele.AlleleName);

            HlaLookupRepository
                .GetHlaLookupTableEntityIfExists(MatchedLocus, Arg.Any<string>(), TypingMethod.Molecular)
                .Returns(firstEntry, secondEntry);

            var actualResult = await LookupService.GetHlaLookupResult(MatchedLocus, expectedLookupName);
            var expectedTceGroups = new[]
            {
                FirstTestAllele.TceGroup,
                SecondTestAllele.TceGroup
            };

            actualResult.TceGroups.ShouldBeEquivalentTo(expectedTceGroups);
        }

        protected override HlaLookupTableEntity BuildTableEntityForSingleAllele(string alleleName)
        {
            var tceGroup = GetTceGroupFromTestAlleles(alleleName);
            var lookupResult = new Dpb1TceGroupsLookupResult(alleleName, tceGroup);

            return new HlaLookupTableEntity(lookupResult);
        }

        private static IEnumerable<string> GetTceGroupFromTestAlleles(string alleleName)
        {
            var testAllele = TestAlleles.SingleOrDefault(allele => allele.AlleleName.Equals(alleleName));

            return testAllele == null
                ? new List<string>()
                : new List<string> { testAllele.TceGroup };
        }
    }
}

