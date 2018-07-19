using Nova.HLAService.Client;
using Nova.HLAService.Client.Models;
using Nova.HLAService.Client.Services;
using Nova.SearchAlgorithm.MatchingDictionary.Exceptions;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage;
using Nova.SearchAlgorithm.MatchingDictionary.Services;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NSubstitute.ReturnsExtensions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Services.Lookups
{
    public abstract class HlaSearchingLookupServiceTestBase<TRepository, TService, TLookupResult>
        where TRepository : class, IHlaLookupRepository
        where TService : IHlaSearchingLookupService<TLookupResult>
        where TLookupResult : IHlaLookupResult
    {
        protected TRepository HlaLookupRepository = Substitute.For<TRepository>();
        protected IAlleleNamesLookupService AlleleNamesLookupService;
        protected IHlaServiceClient HlaServiceClient;
        protected IHlaCategorisationService HlaCategorisationService;
        protected IAlleleStringSplitterService AlleleStringSplitterService;
        protected TService LookupService;

        protected const MolecularLocusType MolecularLocus = MolecularLocusType.A;
        protected const MatchLocus MatchedLocus = MatchLocus.A;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            HlaLookupRepository = Substitute.For<TRepository>();
            HlaServiceClient = Substitute.For<IHlaServiceClient>();
            AlleleNamesLookupService = Substitute.For<IAlleleNamesLookupService>();
            HlaCategorisationService = Substitute.For<IHlaCategorisationService>();
            AlleleStringSplitterService = Substitute.For<IAlleleStringSplitterService>();
        }

        [SetUp]
        public void SetupBeforeEachTest()
        {
            HlaLookupRepository.ClearReceivedCalls();

            var fakeEntityToPreventInvalidHlaExceptionBeingRaised = BuildTableEntityForSingleAllele("alleleName");

            HlaLookupRepository
                .GetHlaLookupTableEntityIfExists(MatchedLocus, Arg.Any<string>(), Arg.Any<TypingMethod>())
                .Returns(fakeEntityToPreventInvalidHlaExceptionBeingRaised);
        }

        [TestCase(HlaTypingCategory.GGroup)]
        [TestCase(HlaTypingCategory.PGroup)]
        public void GetHlaLookupResult_WhenUnhandledHlaTypingCategory_ExceptionIsThrown(HlaTypingCategory category)
        {
            const string hlaName = "HLATYPING";
            HlaCategorisationService.GetHlaTypingCategory(hlaName).Returns(category);

            Assert.ThrowsAsync<MatchingDictionaryException>(
                async () => await LookupService.GetHlaLookupResult(MatchedLocus, hlaName));
        }

        [Test]
        public void GetHlaLookupResult_WhenInvalidHlaTyping_ExceptionIsThrown()
        {
            const string hlaName = "XYZ:123:INVALID";
            HlaCategorisationService.GetHlaTypingCategory(hlaName).Throws(new Exception());

            Assert.ThrowsAsync<MatchingDictionaryException>(
                async () => await LookupService.GetHlaLookupResult(MatchedLocus, hlaName));
        }

        [Test]
        public async Task GetHlaLookupResult_WhenNmdpCode_LookupTheExpandedAlleleList()
        {
            const string hlaName = "99:NMDPCODE";
            const string firstAllele = "99:01";
            const string secondAllele = "100:01";

            HlaCategorisationService.GetHlaTypingCategory(hlaName).Returns(HlaTypingCategory.NmdpCode);
            HlaServiceClient.GetAllelesForDefinedNmdpCode(MolecularLocus, hlaName).Returns(new List<string> { firstAllele, secondAllele });

            await LookupService.GetHlaLookupResult(MatchedLocus, hlaName);

            await HlaLookupRepository.Received(2).GetHlaLookupTableEntityIfExists(
                MatchedLocus, 
                Arg.Is<string>(x => x.Equals(firstAllele) || x.Equals(secondAllele)), 
                TypingMethod.Molecular);
        }

        [TestCase(HlaTypingCategory.AlleleStringOfSubtypes, "Family:Subtype1/Subtype2", "Family:Subtype1", "Family:Subtype2")]
        [TestCase(HlaTypingCategory.AlleleStringOfNames, "Allele1/Allele2", "Allele1", "Allele2")]
        public async Task GetHlaLookupResult_WhenAlleleString_LookupTheAlleleList(
            HlaTypingCategory typingCategory, string hlaName, string firstAllele, string secondAllele)
        {
            HlaCategorisationService.GetHlaTypingCategory(hlaName).Returns(typingCategory);
            AlleleStringSplitterService.GetAlleleNamesFromAlleleString(hlaName).Returns(new List<string> { firstAllele, secondAllele });

            await LookupService.GetHlaLookupResult(MatchedLocus, hlaName);

            await HlaLookupRepository.Received(2).GetHlaLookupTableEntityIfExists(
                MatchedLocus, 
                Arg.Is<string>(x => x.Equals(firstAllele) || x.Equals(secondAllele)), 
                TypingMethod.Molecular);
        }

        [TestCase("99:XX")]
        [TestCase("*99:XX")]
        public async Task GetHlaLookupResult_WhenXxCode_LookupTheFirstField(string hlaName)
        {
            const string firstField = "99";
            HlaCategorisationService.GetHlaTypingCategory(hlaName).Returns(HlaTypingCategory.XxCode);

            await LookupService.GetHlaLookupResult(MatchedLocus, hlaName);

            await HlaLookupRepository.Received().GetHlaLookupTableEntityIfExists(MatchedLocus, firstField, TypingMethod.Molecular);
        }

        [TestCase("*AlleleName", "AlleleName")]
        [TestCase("AlleleName", "AlleleName")]
        public async Task GetHlaLookupResult_WhenAllele_LookupTheSubmittedHlaName(
            string hlaName, string lookupName)
        {
            HlaCategorisationService.GetHlaTypingCategory(hlaName).Returns(HlaTypingCategory.Allele);

            await LookupService.GetHlaLookupResult(MatchedLocus, hlaName);

            await HlaLookupRepository.Received().GetHlaLookupTableEntityIfExists(MatchedLocus, lookupName, TypingMethod.Molecular);
        }

        [Test]
        public async Task GetHlaLookupResult_WhenSubmittedAlleleNameNotFound_LookupTheCurrentName()
        {
            const string submittedAlleleName = "SUBMITTED-NAME";
            const string currentAlleleName = "CURRENT-NAME";

            HlaCategorisationService.GetHlaTypingCategory(submittedAlleleName)
                .Returns(HlaTypingCategory.Allele);

            AlleleNamesLookupService.GetCurrentAlleleNames(MatchedLocus, submittedAlleleName)
                .Returns(Task.FromResult((IEnumerable<string>)new[] { currentAlleleName }));

            // return null on submitted name to emulate scenario that requires a two-field-name lookup
            HlaLookupRepository
                .GetHlaLookupTableEntityIfExists(MatchedLocus, submittedAlleleName, TypingMethod.Molecular)
                .ReturnsNull();
            // return fake entity for current name to prevent invalid hla exception
            var entityFromCurrentName = BuildTableEntityForSingleAllele(currentAlleleName);
            HlaLookupRepository
                .GetHlaLookupTableEntityIfExists(MatchedLocus, currentAlleleName, TypingMethod.Molecular)
                .Returns(entityFromCurrentName);

            await LookupService.GetHlaLookupResult(MatchedLocus, submittedAlleleName);

            await HlaLookupRepository.Received().GetHlaLookupTableEntityIfExists(MatchedLocus, currentAlleleName, TypingMethod.Molecular);
        }

        [Test]
        public async Task GetHlaLookupResult_WhenSubmittedAlleleNameIsFound_DoNotLookupTheCurrentName()
        {
            const string submittedAlleleName = "SUBMITTED-NAME";
            const string currentAlleleName = "CURRENT-NAME";

            HlaCategorisationService.GetHlaTypingCategory(submittedAlleleName)
                .Returns(HlaTypingCategory.Allele);

            AlleleNamesLookupService.GetCurrentAlleleNames(MatchedLocus, submittedAlleleName)
                .Returns(Task.FromResult((IEnumerable<string>)new[] { currentAlleleName }));

            var entityBasedOnLookupName = BuildTableEntityForSingleAllele(submittedAlleleName);
            HlaLookupRepository
                .GetHlaLookupTableEntityIfExists(MatchedLocus, submittedAlleleName, TypingMethod.Molecular)
                .Returns(entityBasedOnLookupName);

            await LookupService.GetHlaLookupResult(MatchedLocus, submittedAlleleName);

            await HlaLookupRepository.DidNotReceive().GetHlaLookupTableEntityIfExists(MatchedLocus, currentAlleleName, TypingMethod.Molecular);
        }

        [Test]
        public async Task GetHlaLookupResult_WhenSerology_LookupTheSubmittedHlaName()
        {
            const string hlaName = "SerologyName";
            HlaCategorisationService.GetHlaTypingCategory(hlaName).Returns(HlaTypingCategory.Serology);

            await LookupService.GetHlaLookupResult(MatchedLocus, hlaName);

            await HlaLookupRepository.Received().GetHlaLookupTableEntityIfExists(MatchedLocus, hlaName, TypingMethod.Serology);
        }

        [Test]
        public void GetHlaLookupResult_WhenNmdpCodeIsInvalid_ExceptionIsThrown()
        {
            const string hlaName = "99:INVALIDCODE";

            HlaCategorisationService.GetHlaTypingCategory(hlaName)
                .Returns(HlaTypingCategory.NmdpCode);
            HlaServiceClient.GetAllelesForDefinedNmdpCode(MolecularLocus, hlaName)
                .Returns<Task<List<string>>>(x => throw new Exception());

            Assert.ThrowsAsync<MatchingDictionaryException>(async () => await LookupService.GetHlaLookupResult(MatchedLocus, hlaName));
        }

        [Test]
        public void GetHlaLookupResult_WhenNmdpCodeContainsAlleleNotInRepository_ExceptionIsThrown()
        {
            const string hlaName = "99:NMDPCODE";
            const string alleleInRepo = "99:01";
            const string alleleNotInRepo = "100:01";
            var entity = BuildTableEntityForSingleAllele(hlaName);

            HlaCategorisationService.GetHlaTypingCategory(hlaName).Returns(HlaTypingCategory.NmdpCode);
            HlaServiceClient.GetAllelesForDefinedNmdpCode(MolecularLocus, hlaName).Returns(new List<string> { alleleInRepo, alleleNotInRepo });

            AlleleNamesLookupService.GetCurrentAlleleNames(MatchedLocus, alleleInRepo)
                .Returns(Task.FromResult((IEnumerable<string>)new[] { alleleInRepo }));
            AlleleNamesLookupService.GetCurrentAlleleNames(MatchedLocus, alleleNotInRepo)
                .Returns(Task.FromResult((IEnumerable<string>)new[] { alleleNotInRepo }));

            HlaLookupRepository.GetHlaLookupTableEntityIfExists(MatchedLocus, alleleInRepo, TypingMethod.Molecular).Returns(entity);
            HlaLookupRepository.GetHlaLookupTableEntityIfExists(MatchedLocus, alleleNotInRepo, TypingMethod.Molecular).ReturnsNull();

            Assert.ThrowsAsync<MatchingDictionaryException>(async () => await LookupService.GetHlaLookupResult(MatchedLocus, hlaName));
        }

        [TestCase(HlaTypingCategory.AlleleStringOfSubtypes, "Family:Subtype1/Subtype2", "Family:Subtype1", "Family:Subtype2")]
        [TestCase(HlaTypingCategory.AlleleStringOfNames, "Allele1/Allele2", "Allele1", "Allele2")]
        public void GetHlaLookupResult_WhenAlleleStringContainsAlleleNotInRepository_ExceptionIsThrown(
            HlaTypingCategory category, string hlaName, string alleleInRepo, string alleleNotInRepo)
        {
            var entity = BuildTableEntityForSingleAllele(hlaName);

            HlaCategorisationService.GetHlaTypingCategory(hlaName)
                .Returns(category);

            AlleleStringSplitterService.GetAlleleNamesFromAlleleString(hlaName)
                .Returns(new List<string> { alleleInRepo, alleleNotInRepo });

            AlleleNamesLookupService.GetCurrentAlleleNames(MatchedLocus, alleleInRepo)
                .Returns(Task.FromResult((IEnumerable<string>)new[] { alleleInRepo }));
            AlleleNamesLookupService.GetCurrentAlleleNames(MatchedLocus, alleleNotInRepo)
                .Returns(Task.FromResult((IEnumerable<string>)new[] { alleleNotInRepo }));

            HlaLookupRepository.GetHlaLookupTableEntityIfExists(MatchedLocus, alleleInRepo, TypingMethod.Molecular)
                .Returns(entity);
            HlaLookupRepository.GetHlaLookupTableEntityIfExists(MatchedLocus, alleleNotInRepo, TypingMethod.Molecular)
                .ReturnsNull();

            Assert.ThrowsAsync<MatchingDictionaryException>(async () => await LookupService.GetHlaLookupResult(MatchedLocus, hlaName));
        }

        protected abstract HlaLookupTableEntity BuildTableEntityForSingleAllele(string alleleName);
    }
}
