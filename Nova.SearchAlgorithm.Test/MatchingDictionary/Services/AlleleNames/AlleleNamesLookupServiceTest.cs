using System.Collections.Generic;
using Nova.SearchAlgorithm.MatchingDictionary.Exceptions;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using Nova.SearchAlgorithm.MatchingDictionary.Services;
using NSubstitute;
using NUnit.Framework;
using System.Threading.Tasks;
using Nova.HLAService.Client.Models;
using Nova.HLAService.Client.Services;
using Nova.SearchAlgorithm.MatchingDictionary.Models.AlleleNames;

namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Services.AlleleNames
{
    [TestFixture]
    public class AlleleNamesLookupServiceTest
    {
        private IAlleleNamesLookupService lookupService;
        private IAlleleNamesRepository repository;
        private IHlaCategorisationService hlaCategorisationService;
        private const MatchLocus MatchedLocus = MatchLocus.A;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            repository = Substitute.For<IAlleleNamesRepository>();
            hlaCategorisationService = Substitute.For<IHlaCategorisationService>();
            lookupService = new AlleleNamesLookupService(repository, hlaCategorisationService);
        }

        [SetUp]
        public void SetupBeforeEachTest()
        {
            repository.ClearReceivedCalls();

            repository
                .GetAlleleNameIfExists(MatchedLocus, Arg.Any<string>())
                .Returns(new AlleleNameEntry(MatchedLocus, "FAKE-ALLELE-TO-PREVENT-INVALID-HLA-EXCEPTION", new List<string>()));
        }

        [TestCase(null)]
        [TestCase("")]
        public void AlleleNamesLookupService_GetCurrentAlleleNames_ExceptionIsThrownWhenStringNullOrEmpty(string nullOrEmptyString)
        {
            Assert.ThrowsAsync<MatchingDictionaryHttpException>(
                async () => await lookupService.GetCurrentAlleleNames(MatchedLocus, nullOrEmptyString));
        }

        public void AlleleNamesLookupService_GetCurrentAlleleNames_ExceptionIsThrownWhenNotAlleleTyping()
        {
            const string notAlleleName = "NOT-AN-ALLELE";
            const HlaTypingCategory notAlleleTypingCategory = HlaTypingCategory.Serology;

            hlaCategorisationService.GetHlaTypingCategory(notAlleleName).Returns(notAlleleTypingCategory);

            Assert.ThrowsAsync<MatchingDictionaryHttpException>(
                async () => await lookupService.GetCurrentAlleleNames(MatchedLocus, notAlleleName));
        }

        [TestCase("*AlleleName", "AlleleName")]
        [TestCase("AlleleName", "AlleleName")]
        public async Task AlleleNamesLookupService_GetCurrentAlleleNames_LookupTheTrimmedAlleleName(
            string submittedLookupName, string trimmedLookupName)
        {
            hlaCategorisationService.GetHlaTypingCategory(Arg.Any<string>()).Returns(HlaTypingCategory.Allele);

            await lookupService.GetCurrentAlleleNames(MatchedLocus, submittedLookupName);

            await repository.Received().GetAlleleNameIfExists(MatchedLocus, trimmedLookupName);
        }
    }
}
