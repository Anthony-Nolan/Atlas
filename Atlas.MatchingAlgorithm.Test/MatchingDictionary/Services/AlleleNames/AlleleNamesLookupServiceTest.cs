using Atlas.Utils.Hla.Services;
using Atlas.Utils.Hla.Models;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.HlaMetadataDictionary.Exceptions;
using Atlas.HlaMetadataDictionary.Models.Lookups.AlleleNameLookup;
using Atlas.HlaMetadataDictionary.Repositories;
using Atlas.HlaMetadataDictionary.Services;
using NSubstitute;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Utils.Models;

namespace Atlas.MatchingAlgorithm.Test.MatchingDictionary.Services.AlleleNames
{
    [TestFixture]
    public class AlleleNamesLookupServiceTest
    {
        private IAlleleNamesLookupService lookupService;
        private IAlleleNamesLookupRepository lookupRepository;
        private IHlaCategorisationService hlaCategorisationService;
        private const Locus MatchedLocus = Locus.A;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            lookupRepository = Substitute.For<IAlleleNamesLookupRepository>();
            hlaCategorisationService = Substitute.For<IHlaCategorisationService>();
            lookupService = new AlleleNamesLookupService(lookupRepository, hlaCategorisationService);
        }

        [SetUp]
        public void SetupBeforeEachTest()
        {
            lookupRepository.ClearReceivedCalls();

            lookupRepository
                .GetAlleleNameIfExists(MatchedLocus, Arg.Any<string>(), Arg.Any<string>())
                .Returns(new AlleleNameLookupResult(MatchedLocus, "FAKE-ALLELE-TO-PREVENT-INVALID-HLA-EXCEPTION", new List<string>()));
        }

        [TestCase(null)]
        [TestCase("")]
        public void GetCurrentAlleleNames_WhenStringNullOrEmpty_ThrowsException(string nullOrEmptyString)
        {
            Assert.ThrowsAsync<MatchingDictionaryException>(
                async () => await lookupService.GetCurrentAlleleNames(MatchedLocus, nullOrEmptyString, "hla-db-version"));
        }

        [Test]
        public void GetCurrentAlleleNames_WhenNotAlleleTyping_ThrowsException()
        {
            const string notAlleleName = "NOT-AN-ALLELE";
            const HlaTypingCategory notAlleleTypingCategory = HlaTypingCategory.Serology;

            hlaCategorisationService.GetHlaTypingCategory(notAlleleName).Returns(notAlleleTypingCategory);

            Assert.ThrowsAsync<MatchingDictionaryException>(
                async () => await lookupService.GetCurrentAlleleNames(MatchedLocus, notAlleleName, "hla-db-version"));
        }

        [TestCase("*AlleleName", "AlleleName")]
        [TestCase("AlleleName", "AlleleName")]
        public async Task GetCurrentAlleleNames_WhenAlleleTyping_LooksUpTheTrimmedAlleleName(
            string submittedLookupName, string trimmedLookupName)
        {
            hlaCategorisationService.GetHlaTypingCategory(Arg.Any<string>()).Returns(HlaTypingCategory.Allele);

            const string hlaDatabaseVersion = "3333";
            await lookupService.GetCurrentAlleleNames(MatchedLocus, submittedLookupName, hlaDatabaseVersion);

            await lookupRepository.Received().GetAlleleNameIfExists(MatchedLocus, trimmedLookupName, hlaDatabaseVersion);
        }
    }
}
