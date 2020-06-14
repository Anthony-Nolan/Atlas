using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.Common.GeneticData.Hla.Services;
using Atlas.HlaMetadataDictionary.ExternalInterface.Exceptions;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata;
using Atlas.HlaMetadataDictionary.InternalModels.MetadataTableRows;
using Atlas.HlaMetadataDictionary.Repositories.MetadataRepositories;
using Atlas.HlaMetadataDictionary.Services.DataRetrieval;
using Atlas.MultipleAlleleCodeDictionary;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
using NUnit.Framework;

namespace Atlas.HlaMetadataDictionary.Test.UnitTests.Services.DataRetrieval.MetadataServices
{
    /// <summary>
    /// Fixture testing the base functionality of HlaSearchingMetadataService
    /// via an arbitrarily chosen base class.
    /// </summary>
    public class HlaSearchingMetadataServiceTests
    {
        private const Locus DefaultLocus = Locus.A;

        private IHlaSearchingMetadataService<IHlaMatchingMetadata> metadataService;

        private IHlaMatchingMetadataRepository hlaMetadataRepository;
        private IAlleleNamesMetadataService alleleNamesMetadataService;
        private IHlaCategorisationService hlaCategorisationService;
        private IAlleleStringSplitterService alleleStringSplitterService;
        private INmdpCodeCache cache;

        [SetUp]
        public void SetUp()
        {
            hlaMetadataRepository = Substitute.For<IHlaMatchingMetadataRepository>();
            alleleNamesMetadataService = Substitute.For<IAlleleNamesMetadataService>();
            hlaCategorisationService = Substitute.For<IHlaCategorisationService>();
            alleleStringSplitterService = Substitute.For<IAlleleStringSplitterService>();
            cache = Substitute.For<INmdpCodeCache>();

            metadataService = new HlaMatchingMetadataService(
                hlaMetadataRepository,
                alleleNamesMetadataService,
                hlaCategorisationService,
                alleleStringSplitterService,
                cache);

            var fakeEntityToPreventInvalidHlaExceptionBeingRaised = BuildMetadataRowForSingleAllele("alleleName");

            hlaMetadataRepository
                .GetHlaMetadataRowIfExists(DefaultLocus, Arg.Any<string>(), Arg.Any<TypingMethod>(), Arg.Any<string>())
                .Returns(fakeEntityToPreventInvalidHlaExceptionBeingRaised);
        }

        [TestCase(HlaTypingCategory.GGroup)]
        [TestCase(HlaTypingCategory.PGroup)]
        public void GetHlaMetadata_WhenUnhandledHlaTypingCategory_ExceptionIsThrown(HlaTypingCategory category)
        {
            const string hlaName = "HLATYPING";
            hlaCategorisationService.GetHlaTypingCategory(hlaName).Returns(category);

            Assert.ThrowsAsync<HlaMetadataDictionaryException>(
                async () => await metadataService.GetHlaMetadata(DefaultLocus, hlaName, "hla-db-version"));
        }

        [TestCase(HlaTypingCategory.AlleleStringOfSubtypes, "Family:Subtype1/Subtype2", "Family:Subtype1", "Family:Subtype2")]
        [TestCase(HlaTypingCategory.AlleleStringOfNames, "Allele1/Allele2", "Allele1", "Allele2")]
        public async Task GetHlaMetadata_WhenAlleleString_LookupTheAlleleList(
            HlaTypingCategory typingCategory,
            string hlaName,
            string firstAllele,
            string secondAllele)
        {
            hlaCategorisationService.GetHlaTypingCategory(hlaName).Returns(typingCategory);
            alleleStringSplitterService.GetAlleleNamesFromAlleleString(hlaName).Returns(new List<string> {firstAllele, secondAllele});

            await metadataService.GetHlaMetadata(DefaultLocus, hlaName, "hla-db-version");

            await hlaMetadataRepository.Received(2).GetHlaMetadataRowIfExists(
                DefaultLocus,
                Arg.Is<string>(x => x.Equals(firstAllele) || x.Equals(secondAllele)),
                TypingMethod.Molecular, Arg.Any<string>());
        }

        [TestCase("99:XX", "99:XX")]
        [TestCase("*99:XX", "99:XX")]
        public async Task GetHlaMetadata_WhenXxCode_LookupTheSubmittedHlaName(string hlaName, string lookupName)
        {
            hlaCategorisationService.GetHlaTypingCategory(lookupName).Returns(HlaTypingCategory.XxCode);

            await metadataService.GetHlaMetadata(DefaultLocus, hlaName, "hla-db-version");

            await hlaMetadataRepository.Received().GetHlaMetadataRowIfExists(DefaultLocus, lookupName, TypingMethod.Molecular, Arg.Any<string>());
        }

        [TestCase("*AlleleName", "AlleleName")]
        [TestCase("AlleleName", "AlleleName")]
        public async Task GetHlaMetadata_WhenAllele_LookupTheSubmittedHlaName(string hlaName, string lookupName)
        {
            hlaCategorisationService.GetHlaTypingCategory(hlaName).Returns(HlaTypingCategory.Allele);

            await metadataService.GetHlaMetadata(DefaultLocus, hlaName, "hla-db-version");

            await hlaMetadataRepository.Received().GetHlaMetadataRowIfExists(DefaultLocus, lookupName, TypingMethod.Molecular, Arg.Any<string>());
        }

        [Test]
        public async Task GetHlaMetadata_WhenSubmittedAlleleNameNotFound_LookupTheCurrentName()
        {
            const string submittedAlleleName = "SUBMITTED-NAME";
            const string currentAlleleName = "CURRENT-NAME";

            hlaCategorisationService.GetHlaTypingCategory(submittedAlleleName)
                .Returns(HlaTypingCategory.Allele);

            alleleNamesMetadataService.GetCurrentAlleleNames(DefaultLocus, submittedAlleleName, Arg.Any<string>())
                .Returns(Task.FromResult((IEnumerable<string>) new[] {currentAlleleName}));

            // return null on submitted name to emulate scenario that requires a current name lookup
            hlaMetadataRepository
                .GetHlaMetadataRowIfExists(DefaultLocus, submittedAlleleName, TypingMethod.Molecular, Arg.Any<string>())
                .ReturnsNull();
            // return fake entity for current name to prevent invalid hla exception
            var entityFromCurrentName = BuildMetadataRowForSingleAllele(currentAlleleName);
            hlaMetadataRepository
                .GetHlaMetadataRowIfExists(DefaultLocus, currentAlleleName, TypingMethod.Molecular, Arg.Any<string>())
                .Returns(entityFromCurrentName);

            await metadataService.GetHlaMetadata(DefaultLocus, submittedAlleleName, "hla-db-version");

            await hlaMetadataRepository.Received()
                .GetHlaMetadataRowIfExists(DefaultLocus, currentAlleleName, TypingMethod.Molecular, Arg.Any<string>());
        }

        [Test]
        public async Task GetHlaMetadata_WhenSubmittedAlleleNameIsFound_DoNotLookupTheCurrentName()
        {
            const string submittedAlleleName = "SUBMITTED-NAME";
            const string currentAlleleName = "CURRENT-NAME";

            hlaCategorisationService.GetHlaTypingCategory(submittedAlleleName)
                .Returns(HlaTypingCategory.Allele);

            alleleNamesMetadataService.GetCurrentAlleleNames(DefaultLocus, submittedAlleleName, Arg.Any<string>())
                .Returns(Task.FromResult((IEnumerable<string>) new[] {currentAlleleName}));

            var entityBasedOnLookupName = BuildMetadataRowForSingleAllele(submittedAlleleName);
            hlaMetadataRepository
                .GetHlaMetadataRowIfExists(DefaultLocus, submittedAlleleName, TypingMethod.Molecular, Arg.Any<string>())
                .Returns(entityBasedOnLookupName);

            await metadataService.GetHlaMetadata(DefaultLocus, submittedAlleleName, "hla-db-version");

            await hlaMetadataRepository.DidNotReceive()
                .GetHlaMetadataRowIfExists(DefaultLocus, currentAlleleName, TypingMethod.Molecular, Arg.Any<string>());
        }

        [Test]
        public async Task GetHlaMetadata_WhenSerology_LookupTheSubmittedHlaName()
        {
            const string hlaName = "SerologyName";
            hlaCategorisationService.GetHlaTypingCategory(hlaName).Returns(HlaTypingCategory.Serology);

            await metadataService.GetHlaMetadata(DefaultLocus, hlaName, "hla-db-version");

            await hlaMetadataRepository.Received().GetHlaMetadataRowIfExists(DefaultLocus, hlaName, TypingMethod.Serology, Arg.Any<string>());
        }

        private static HlaMetadataTableRow BuildMetadataRowForSingleAllele(string alleleName)
        {
            var metadata = new HlaMatchingMetadata(
                DefaultLocus,
                alleleName,
                TypingMethod.Molecular,
                new List<string> { alleleName }
            );

            return new HlaMetadataTableRow(metadata);
        }
    }
}