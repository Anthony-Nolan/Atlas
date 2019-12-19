using Nova.HLAService.Client.Models;
using Nova.HLAService.Client.Services;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.MatchingDictionary.Caching;
using Nova.SearchAlgorithm.MatchingDictionary.Exceptions;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.MatchingLookup;
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
    /// <summary>
    /// Fixture testing the base functionality of HlaSearchingLookupService
    /// via an arbitrarily chosen base class.
    /// </summary>
    public class HlaSearchingLookupServiceTests
    {
        private const Locus DefaultLocus = Locus.A;

        private IHlaSearchingLookupService<IHlaMatchingLookupResult> lookupService;

        private IHlaMatchingLookupRepository hlaLookupRepository;
        private IAlleleNamesLookupService alleleNamesLookupService;
        private IHlaCategorisationService hlaCategorisationService;
        private IAlleleStringSplitterService alleleStringSplitterService;
        private INmdpCodeCache cache;

        [SetUp]
        public void SetUp()
        {
            hlaLookupRepository = Substitute.For<IHlaMatchingLookupRepository>();
            alleleNamesLookupService = Substitute.For<IAlleleNamesLookupService>();
            hlaCategorisationService = Substitute.For<IHlaCategorisationService>();
            alleleStringSplitterService = Substitute.For<IAlleleStringSplitterService>();
            cache = Substitute.For<INmdpCodeCache>();

            lookupService = new HlaMatchingLookupService(
                hlaLookupRepository,
                alleleNamesLookupService,
                hlaCategorisationService,
                alleleStringSplitterService,
                cache);

            var fakeEntityToPreventInvalidHlaExceptionBeingRaised = BuildTableEntityForSingleAllele("alleleName");

            hlaLookupRepository
                .GetHlaLookupTableEntityIfExists(DefaultLocus, Arg.Any<string>(), Arg.Any<TypingMethod>(), Arg.Any<string>())
                .Returns(fakeEntityToPreventInvalidHlaExceptionBeingRaised);
        }

        [TestCase(HlaTypingCategory.GGroup)]
        [TestCase(HlaTypingCategory.PGroup)]
        public void GetHlaLookupResult_WhenUnhandledHlaTypingCategory_ExceptionIsThrown(HlaTypingCategory category)
        {
            const string hlaName = "HLATYPING";
            hlaCategorisationService.GetHlaTypingCategory(hlaName).Returns(category);

            Assert.ThrowsAsync<MatchingDictionaryException>(
                async () => await lookupService.GetHlaLookupResult(DefaultLocus, hlaName, "hla-db-version"));
        }

        [TestCase(HlaTypingCategory.AlleleStringOfSubtypes, "Family:Subtype1/Subtype2", "Family:Subtype1", "Family:Subtype2")]
        [TestCase(HlaTypingCategory.AlleleStringOfNames, "Allele1/Allele2", "Allele1", "Allele2")]
        public async Task GetHlaLookupResult_WhenAlleleString_LookupTheAlleleList(
            HlaTypingCategory typingCategory,
            string hlaName,
            string firstAllele,
            string secondAllele)
        {
            hlaCategorisationService.GetHlaTypingCategory(hlaName).Returns(typingCategory);
            alleleStringSplitterService.GetAlleleNamesFromAlleleString(hlaName).Returns(new List<string> {firstAllele, secondAllele});

            await lookupService.GetHlaLookupResult(DefaultLocus, hlaName, "hla-db-version");

            await hlaLookupRepository.Received(2).GetHlaLookupTableEntityIfExists(
                DefaultLocus,
                Arg.Is<string>(x => x.Equals(firstAllele) || x.Equals(secondAllele)),
                TypingMethod.Molecular, Arg.Any<string>());
        }

        [TestCase("99:XX", "99:XX")]
        [TestCase("*99:XX", "99:XX")]
        public async Task GetHlaLookupResult_WhenXxCode_LookupTheSubmittedHlaName(string hlaName, string lookupName)
        {
            hlaCategorisationService.GetHlaTypingCategory(lookupName).Returns(HlaTypingCategory.XxCode);

            await lookupService.GetHlaLookupResult(DefaultLocus, hlaName, "hla-db-version");

            await hlaLookupRepository.Received().GetHlaLookupTableEntityIfExists(DefaultLocus, lookupName, TypingMethod.Molecular, Arg.Any<string>());
        }

        [TestCase("*AlleleName", "AlleleName")]
        [TestCase("AlleleName", "AlleleName")]
        public async Task GetHlaLookupResult_WhenAllele_LookupTheSubmittedHlaName(string hlaName, string lookupName)
        {
            hlaCategorisationService.GetHlaTypingCategory(hlaName).Returns(HlaTypingCategory.Allele);

            await lookupService.GetHlaLookupResult(DefaultLocus, hlaName, "hla-db-version");

            await hlaLookupRepository.Received().GetHlaLookupTableEntityIfExists(DefaultLocus, lookupName, TypingMethod.Molecular, Arg.Any<string>());
        }

        [Test]
        public async Task GetHlaLookupResult_WhenSubmittedAlleleNameNotFound_LookupTheCurrentName()
        {
            const string submittedAlleleName = "SUBMITTED-NAME";
            const string currentAlleleName = "CURRENT-NAME";

            hlaCategorisationService.GetHlaTypingCategory(submittedAlleleName)
                .Returns(HlaTypingCategory.Allele);

            alleleNamesLookupService.GetCurrentAlleleNames(DefaultLocus, submittedAlleleName, Arg.Any<string>())
                .Returns(Task.FromResult((IEnumerable<string>) new[] {currentAlleleName}));

            // return null on submitted name to emulate scenario that requires a current name lookup
            hlaLookupRepository
                .GetHlaLookupTableEntityIfExists(DefaultLocus, submittedAlleleName, TypingMethod.Molecular, Arg.Any<string>())
                .ReturnsNull();
            // return fake entity for current name to prevent invalid hla exception
            var entityFromCurrentName = BuildTableEntityForSingleAllele(currentAlleleName);
            hlaLookupRepository
                .GetHlaLookupTableEntityIfExists(DefaultLocus, currentAlleleName, TypingMethod.Molecular, Arg.Any<string>())
                .Returns(entityFromCurrentName);

            await lookupService.GetHlaLookupResult(DefaultLocus, submittedAlleleName, "hla-db-version");

            await hlaLookupRepository.Received()
                .GetHlaLookupTableEntityIfExists(DefaultLocus, currentAlleleName, TypingMethod.Molecular, Arg.Any<string>());
        }

        [Test]
        public async Task GetHlaLookupResult_WhenSubmittedAlleleNameIsFound_DoNotLookupTheCurrentName()
        {
            const string submittedAlleleName = "SUBMITTED-NAME";
            const string currentAlleleName = "CURRENT-NAME";

            hlaCategorisationService.GetHlaTypingCategory(submittedAlleleName)
                .Returns(HlaTypingCategory.Allele);

            alleleNamesLookupService.GetCurrentAlleleNames(DefaultLocus, submittedAlleleName, Arg.Any<string>())
                .Returns(Task.FromResult((IEnumerable<string>) new[] {currentAlleleName}));

            var entityBasedOnLookupName = BuildTableEntityForSingleAllele(submittedAlleleName);
            hlaLookupRepository
                .GetHlaLookupTableEntityIfExists(DefaultLocus, submittedAlleleName, TypingMethod.Molecular, Arg.Any<string>())
                .Returns(entityBasedOnLookupName);

            await lookupService.GetHlaLookupResult(DefaultLocus, submittedAlleleName, "hla-db-version");

            await hlaLookupRepository.DidNotReceive()
                .GetHlaLookupTableEntityIfExists(DefaultLocus, currentAlleleName, TypingMethod.Molecular, Arg.Any<string>());
        }

        [Test]
        public async Task GetHlaLookupResult_WhenSerology_LookupTheSubmittedHlaName()
        {
            const string hlaName = "SerologyName";
            hlaCategorisationService.GetHlaTypingCategory(hlaName).Returns(HlaTypingCategory.Serology);

            await lookupService.GetHlaLookupResult(DefaultLocus, hlaName, "hla-db-version");

            await hlaLookupRepository.Received().GetHlaLookupTableEntityIfExists(DefaultLocus, hlaName, TypingMethod.Serology, Arg.Any<string>());
        }

        private static HlaLookupTableEntity BuildTableEntityForSingleAllele(string alleleName)
        {
            var lookupResult = new HlaMatchingLookupResult(
                DefaultLocus,
                alleleName,
                TypingMethod.Molecular,
                new List<string> { alleleName }
            );

            return new HlaLookupTableEntity(lookupResult);
        }
    }
}