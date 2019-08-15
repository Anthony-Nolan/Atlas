using Microsoft.Extensions.Caching.Memory;
using Nova.HLAService.Client;
using Nova.HLAService.Client.Models;
using Nova.HLAService.Client.Services;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.MatchingDictionary.Exceptions;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories.AzureStorage;
using Nova.SearchAlgorithm.MatchingDictionary.Services;
using Nova.Utils.ApplicationInsights;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NSubstitute.ReturnsExtensions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LazyCache;

namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Services.Lookups
{
    public abstract class HlaSearchingLookupServiceTestBase<TRepository, TService, TLookupResult>
        where TRepository : class, IHlaLookupRepository
        where TService : IHlaSearchingLookupService<TLookupResult>
        where TLookupResult : IHlaLookupResult
    {
        protected TRepository HlaLookupRepository;
        protected IAlleleNamesLookupService AlleleNamesLookupService;
        protected IHlaServiceClient HlaServiceClient;
        protected IHlaCategorisationService HlaCategorisationService;
        protected IAlleleStringSplitterService AlleleStringSplitterService;
        protected IAppCache Cache;
        protected ILogger Logger;
        protected TService LookupService;

        protected MolecularLocusType MolecularLocus = MolecularLocusType.A;
        protected Locus MatchedLocus = Locus.A;

        [SetUp]
        public void SetUpBeforeEachTest()
        {
            HlaLookupRepository = Substitute.For<TRepository>();
            HlaServiceClient = Substitute.For<IHlaServiceClient>();
            AlleleNamesLookupService = Substitute.For<IAlleleNamesLookupService>();
            HlaCategorisationService = Substitute.For<IHlaCategorisationService>();
            AlleleStringSplitterService = Substitute.For<IAlleleStringSplitterService>();
            Cache = Substitute.For<IAppCache>();
            Logger = Substitute.For<ILogger>();

            var fakeEntityToPreventInvalidHlaExceptionBeingRaised = BuildTableEntityForSingleAllele("alleleName");

            HlaLookupRepository
                .GetHlaLookupTableEntityIfExists(MatchedLocus, Arg.Any<string>(), Arg.Any<TypingMethod>(), Arg.Any<string>())
                .Returns(fakeEntityToPreventInvalidHlaExceptionBeingRaised);
        }

        [TestCase(HlaTypingCategory.GGroup)]
        [TestCase(HlaTypingCategory.PGroup)]
        public void GetHlaLookupResult_WhenUnhandledHlaTypingCategory_ExceptionIsThrown(HlaTypingCategory category)
        {
            const string hlaName = "HLATYPING";
            HlaCategorisationService.GetHlaTypingCategory(hlaName).Returns(category);

            Assert.ThrowsAsync<MatchingDictionaryException>(
                async () => await LookupService.GetHlaLookupResult(MatchedLocus, hlaName, "hla-db-version"));
        }

        [Test]
        public void GetHlaLookupResult_WhenInvalidHlaTyping_ExceptionIsThrown()
        {
            const string hlaName = "XYZ:123:INVALID";
            HlaCategorisationService.GetHlaTypingCategory(hlaName).Throws(new Exception());

            Assert.ThrowsAsync<MatchingDictionaryException>(
                async () => await LookupService.GetHlaLookupResult(MatchedLocus, hlaName, "hla-db-version"));
        }

        [Test]
        public async Task GetHlaLookupResult_WhenNmdpCode_LookupTheExpandedAlleleList()
        {
            const string hlaName = "99:NMDPCODE";
            const string firstAllele = "99:01";
            const string secondAllele = "100:01";

            HlaCategorisationService.GetHlaTypingCategory(hlaName).Returns(HlaTypingCategory.NmdpCode);
            HlaServiceClient.GetAllelesForDefinedNmdpCode(MolecularLocus, hlaName).Returns(new List<string> {firstAllele, secondAllele});

            await LookupService.GetHlaLookupResult(MatchedLocus, hlaName, "hla-db-version");

            await HlaLookupRepository.Received(2).GetHlaLookupTableEntityIfExists(
                MatchedLocus,
                Arg.Is<string>(x => x.Equals(firstAllele) || x.Equals(secondAllele)),
                TypingMethod.Molecular, Arg.Any<string>());
        }

        [TestCase(HlaTypingCategory.AlleleStringOfSubtypes, "Family:Subtype1/Subtype2", "Family:Subtype1", "Family:Subtype2")]
        [TestCase(HlaTypingCategory.AlleleStringOfNames, "Allele1/Allele2", "Allele1", "Allele2")]
        public async Task GetHlaLookupResult_WhenAlleleString_LookupTheAlleleList(
            HlaTypingCategory typingCategory,
            string hlaName,
            string firstAllele,
            string secondAllele)
        {
            HlaCategorisationService.GetHlaTypingCategory(hlaName).Returns(typingCategory);
            AlleleStringSplitterService.GetAlleleNamesFromAlleleString(hlaName).Returns(new List<string> {firstAllele, secondAllele});

            await LookupService.GetHlaLookupResult(MatchedLocus, hlaName, "hla-db-version");

            await HlaLookupRepository.Received(2).GetHlaLookupTableEntityIfExists(
                MatchedLocus,
                Arg.Is<string>(x => x.Equals(firstAllele) || x.Equals(secondAllele)),
                TypingMethod.Molecular, Arg.Any<string>());
        }

        [TestCase("99:XX", "99:XX")]
        [TestCase("*99:XX", "99:XX")]
        public async Task GetHlaLookupResult_WhenXxCode_LookupTheSubmittedHlaName(string hlaName, string lookupName)
        {
            HlaCategorisationService.GetHlaTypingCategory(lookupName).Returns(HlaTypingCategory.XxCode);

            await LookupService.GetHlaLookupResult(MatchedLocus, hlaName, "hla-db-version");

            await HlaLookupRepository.Received().GetHlaLookupTableEntityIfExists(MatchedLocus, lookupName, TypingMethod.Molecular, Arg.Any<string>());
        }

        [TestCase("*AlleleName", "AlleleName")]
        [TestCase("AlleleName", "AlleleName")]
        public async Task GetHlaLookupResult_WhenAllele_LookupTheSubmittedHlaName(string hlaName, string lookupName)
        {
            HlaCategorisationService.GetHlaTypingCategory(hlaName).Returns(HlaTypingCategory.Allele);

            await LookupService.GetHlaLookupResult(MatchedLocus, hlaName, "hla-db-version");

            await HlaLookupRepository.Received().GetHlaLookupTableEntityIfExists(MatchedLocus, lookupName, TypingMethod.Molecular, Arg.Any<string>());
        }

        [Test]
        public async Task GetHlaLookupResult_WhenSubmittedAlleleNameNotFound_LookupTheCurrentName()
        {
            const string submittedAlleleName = "SUBMITTED-NAME";
            const string currentAlleleName = "CURRENT-NAME";

            HlaCategorisationService.GetHlaTypingCategory(submittedAlleleName)
                .Returns(HlaTypingCategory.Allele);

            AlleleNamesLookupService.GetCurrentAlleleNames(MatchedLocus, submittedAlleleName, Arg.Any<string>())
                .Returns(Task.FromResult((IEnumerable<string>) new[] {currentAlleleName}));

            // return null on submitted name to emulate scenario that requires a current name lookup
            HlaLookupRepository
                .GetHlaLookupTableEntityIfExists(MatchedLocus, submittedAlleleName, TypingMethod.Molecular, Arg.Any<string>())
                .ReturnsNull();
            // return fake entity for current name to prevent invalid hla exception
            var entityFromCurrentName = BuildTableEntityForSingleAllele(currentAlleleName);
            HlaLookupRepository
                .GetHlaLookupTableEntityIfExists(MatchedLocus, currentAlleleName, TypingMethod.Molecular, Arg.Any<string>())
                .Returns(entityFromCurrentName);

            await LookupService.GetHlaLookupResult(MatchedLocus, submittedAlleleName, "hla-db-version");

            await HlaLookupRepository.Received()
                .GetHlaLookupTableEntityIfExists(MatchedLocus, currentAlleleName, TypingMethod.Molecular, Arg.Any<string>());
        }

        [Test]
        public async Task GetHlaLookupResult_WhenSubmittedAlleleNameIsFound_DoNotLookupTheCurrentName()
        {
            const string submittedAlleleName = "SUBMITTED-NAME";
            const string currentAlleleName = "CURRENT-NAME";

            HlaCategorisationService.GetHlaTypingCategory(submittedAlleleName)
                .Returns(HlaTypingCategory.Allele);

            AlleleNamesLookupService.GetCurrentAlleleNames(MatchedLocus, submittedAlleleName, Arg.Any<string>())
                .Returns(Task.FromResult((IEnumerable<string>) new[] {currentAlleleName}));

            var entityBasedOnLookupName = BuildTableEntityForSingleAllele(submittedAlleleName);
            HlaLookupRepository
                .GetHlaLookupTableEntityIfExists(MatchedLocus, submittedAlleleName, TypingMethod.Molecular, Arg.Any<string>())
                .Returns(entityBasedOnLookupName);

            await LookupService.GetHlaLookupResult(MatchedLocus, submittedAlleleName, "hla-db-version");

            await HlaLookupRepository.DidNotReceive()
                .GetHlaLookupTableEntityIfExists(MatchedLocus, currentAlleleName, TypingMethod.Molecular, Arg.Any<string>());
        }

        [Test]
        public async Task GetHlaLookupResult_WhenSerology_LookupTheSubmittedHlaName()
        {
            const string hlaName = "SerologyName";
            HlaCategorisationService.GetHlaTypingCategory(hlaName).Returns(HlaTypingCategory.Serology);

            await LookupService.GetHlaLookupResult(MatchedLocus, hlaName, "hla-db-version");

            await HlaLookupRepository.Received().GetHlaLookupTableEntityIfExists(MatchedLocus, hlaName, TypingMethod.Serology, Arg.Any<string>());
        }

        [Test]
        public void GetHlaLookupResult_WhenNmdpCodeIsInvalid_ExceptionIsThrown()
        {
            const string hlaName = "99:INVALIDCODE";

            HlaCategorisationService.GetHlaTypingCategory(hlaName)
                .Returns(HlaTypingCategory.NmdpCode);
            HlaServiceClient.GetAllelesForDefinedNmdpCode(MolecularLocus, hlaName)
                .Returns<Task<List<string>>>(x => throw new Exception());

            Assert.ThrowsAsync<MatchingDictionaryException>(async () => await LookupService.GetHlaLookupResult(MatchedLocus, hlaName, ""));
        }

        [Test]
        public void GetHlaLookupResult_WhenNmdpCodeContainsAlleleNotInRepository_ExceptionIsThrown()
        {
            const string hlaName = "99:NMDPCODE";
            const string alleleInRepo = "99:01";
            const string alleleNotInRepo = "100:01";
            var entity = BuildTableEntityForSingleAllele(hlaName);

            HlaCategorisationService.GetHlaTypingCategory(hlaName).Returns(HlaTypingCategory.NmdpCode);
            HlaServiceClient.GetAllelesForDefinedNmdpCode(MolecularLocus, hlaName).Returns(new List<string> {alleleInRepo, alleleNotInRepo});

            AlleleNamesLookupService.GetCurrentAlleleNames(MatchedLocus, alleleInRepo, Arg.Any<string>())
                .Returns(Task.FromResult((IEnumerable<string>) new[] {alleleInRepo}));
            AlleleNamesLookupService.GetCurrentAlleleNames(MatchedLocus, alleleNotInRepo, Arg.Any<string>())
                .Returns(Task.FromResult((IEnumerable<string>) new[] {alleleNotInRepo}));

            HlaLookupRepository.GetHlaLookupTableEntityIfExists(MatchedLocus, alleleInRepo, TypingMethod.Molecular, Arg.Any<string>())
                .Returns(entity);
            HlaLookupRepository.GetHlaLookupTableEntityIfExists(MatchedLocus, alleleNotInRepo, TypingMethod.Molecular, Arg.Any<string>())
                .ReturnsNull();

            Assert.ThrowsAsync<MatchingDictionaryException>(async () => await LookupService.GetHlaLookupResult(MatchedLocus, hlaName, ""));
        }

        [TestCase(HlaTypingCategory.AlleleStringOfSubtypes, "Family:Subtype1/Subtype2", "Family:Subtype1", "Family:Subtype2")]
        [TestCase(HlaTypingCategory.AlleleStringOfNames, "Allele1/Allele2", "Allele1", "Allele2")]
        public void GetHlaLookupResult_WhenAlleleStringContainsAlleleNotInRepository_ExceptionIsThrown(
            HlaTypingCategory category,
            string hlaName,
            string alleleInRepo,
            string alleleNotInRepo)
        {
            var entity = BuildTableEntityForSingleAllele(hlaName);

            HlaCategorisationService.GetHlaTypingCategory(hlaName)
                .Returns(category);

            AlleleStringSplitterService.GetAlleleNamesFromAlleleString(hlaName)
                .Returns(new List<string> {alleleInRepo, alleleNotInRepo});

            AlleleNamesLookupService.GetCurrentAlleleNames(MatchedLocus, alleleInRepo, Arg.Any<string>())
                .Returns(Task.FromResult((IEnumerable<string>) new[] {alleleInRepo}));
            AlleleNamesLookupService.GetCurrentAlleleNames(MatchedLocus, alleleNotInRepo, Arg.Any<string>())
                .Returns(Task.FromResult((IEnumerable<string>) new[] {alleleNotInRepo}));

            HlaLookupRepository.GetHlaLookupTableEntityIfExists(MatchedLocus, alleleInRepo, TypingMethod.Molecular, Arg.Any<string>())
                .Returns(entity);
            HlaLookupRepository.GetHlaLookupTableEntityIfExists(MatchedLocus, alleleNotInRepo, TypingMethod.Molecular, Arg.Any<string>())
                .ReturnsNull();

            Assert.ThrowsAsync<MatchingDictionaryException>(async () =>
                await LookupService.GetHlaLookupResult(MatchedLocus, hlaName, "hla-db-version"));
        }

        protected abstract HlaLookupTableEntity BuildTableEntityForSingleAllele(string alleleName);
    }
}