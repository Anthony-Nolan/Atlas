using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.Common.GeneticData.Hla.Services;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata;
using Atlas.HlaMetadataDictionary.InternalModels.MetadataTableRows;
using Atlas.HlaMetadataDictionary.Repositories.MetadataRepositories;
using Atlas.HlaMetadataDictionary.Services.DataRetrieval;
using Atlas.MultipleAlleleCodeDictionary.ExternalInterface;
using LazyCache;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.Caching;
using Atlas.Common.Test.SharedTestHelpers.Builders;

namespace Atlas.HlaMetadataDictionary.Test.UnitTests.Services.DataRetrieval.MetadataServices
{
    /// <summary>
    /// Fixture testing the base functionality of HlaSearchingMetadataService
    /// via an arbitrarily chosen base class.
    /// </summary>
    public class SearchRelatedMetadataServiceTests
    {
        private const Locus DefaultLocus = Locus.A;

        private ISearchRelatedMetadataService<IHlaMatchingMetadata> metadataService;

        private IHlaMatchingMetadataRepository hlaMetadataRepository;
        private IAlleleNamesMetadataService alleleNamesMetadataService;
        private IHlaCategorisationService hlaCategorisationService;
        private IAlleleNamesExtractor alleleNamesExtractor;
        private IMacDictionary macDictionary;
        private IAlleleGroupExpander alleleGroupExpander;
        private IAppCache cache;

        [SetUp]
        public void SetUp()
        {
            hlaMetadataRepository = Substitute.For<IHlaMatchingMetadataRepository>();
            alleleNamesMetadataService = Substitute.For<IAlleleNamesMetadataService>();
            hlaCategorisationService = Substitute.For<IHlaCategorisationService>();
            alleleNamesExtractor = Substitute.For<IAlleleNamesExtractor>();
            macDictionary = Substitute.For<IMacDictionary>();
            alleleGroupExpander = Substitute.For<IAlleleGroupExpander>();

            cache = AppCacheBuilder.NewDefaultCache();
            var cacheProvider = Substitute.For<IPersistentCacheProvider>();
            cacheProvider.Cache.Returns(cache);

            metadataService = new HlaMatchingMetadataService(
                hlaMetadataRepository,
                alleleNamesMetadataService,
                hlaCategorisationService,
                alleleNamesExtractor,
                macDictionary,
                alleleGroupExpander,
                cacheProvider);

            #region Set up to prevent exceptions that would incorrectly fail tests
            hlaMetadataRepository
                .GetHlaMetadataRowIfExists(default, default, default, default)
                .ReturnsForAnyArgs(BuildMetadataRowForSingleAllele("alleleName"));

            alleleGroupExpander.ExpandAlleleGroup(default, default, default)
                .ReturnsForAnyArgs(new[] { "allele" });

            macDictionary.GetHlaFromMac(default).ReturnsForAnyArgs(new[] { "allele" });
            #endregion
        }

        [TestCase(HlaTypingCategory.AlleleStringOfSubtypes, "Family:Subtype1/Subtype2", "Family:Subtype1", "Family:Subtype2")]
        [TestCase(HlaTypingCategory.AlleleStringOfNames, "Allele1/Allele2", "Allele1", "Allele2")]
        public async Task GetHlaMetadata_WhenAlleleString_GetsMetadataForAlleleList(
            HlaTypingCategory typingCategory,
            string hlaName,
            string firstAllele,
            string secondAllele)
        {
            hlaCategorisationService.GetHlaTypingCategory(hlaName).Returns(typingCategory);
            alleleNamesExtractor.GetAlleleNamesFromAlleleString(hlaName).Returns(new List<string> { firstAllele, secondAllele });

            await metadataService.GetHlaMetadata(DefaultLocus, hlaName, "hla-db-version");

            await hlaMetadataRepository.Received(2).GetHlaMetadataRowIfExists(
                DefaultLocus,
                Arg.Is<string>(x => x.Equals(firstAllele) || x.Equals(secondAllele)),
                TypingMethod.Molecular, Arg.Any<string>());
        }

        [TestCase]
        public async Task GetHlaMetadata_WhenMac_LooksUpHlaForMac()
        {
            const string mac = "mac";
            hlaCategorisationService.GetHlaTypingCategory(default).ReturnsForAnyArgs(HlaTypingCategory.NmdpCode);

            await metadataService.GetHlaMetadata(DefaultLocus, mac, "hla-db-version");

            await macDictionary.Received().GetHlaFromMac(mac);
        }

        [Test]
        public async Task GetHlaMetadata_WhenMac_GetsMetadataForMacAlleles()
        {
            const string macAllele = "allele";
            hlaCategorisationService.GetHlaTypingCategory(default).ReturnsForAnyArgs(HlaTypingCategory.NmdpCode);
            macDictionary.GetHlaFromMac(default).ReturnsForAnyArgs(new[] { macAllele });

            await metadataService.GetHlaMetadata(DefaultLocus, "mac", "hla-db-version");

            await hlaMetadataRepository.Received(1).GetHlaMetadataRowIfExists(
                DefaultLocus,
                macAllele,
                TypingMethod.Molecular,
                Arg.Any<string>());
        }

        [TestCase(HlaTypingCategory.PGroup)]
        [TestCase(HlaTypingCategory.GGroup)]
        [TestCase(HlaTypingCategory.SmallGGroup)]
        public async Task GetHlaMetadata_WhenAlleleGroup_ExpandsAlleleGroup(HlaTypingCategory typingCategory)
        {
            const string alleleGroupName = "group";
            hlaCategorisationService.GetHlaTypingCategory(default).ReturnsForAnyArgs(typingCategory);

            await metadataService.GetHlaMetadata(DefaultLocus, alleleGroupName, "hla-db-version");

            await alleleGroupExpander.Received()
                .ExpandAlleleGroup(DefaultLocus, alleleGroupName, Arg.Any<string>());
        }

        [TestCase(HlaTypingCategory.PGroup)]
        [TestCase(HlaTypingCategory.GGroup)]
        [TestCase(HlaTypingCategory.SmallGGroup)]
        public async Task GetHlaMetadata_WhenAlleleGroup_GetsMetadataForAllelesInGroup(HlaTypingCategory typingCategory)
        {
            const string alleleInGroup = "allele";
            hlaCategorisationService.GetHlaTypingCategory(default).ReturnsForAnyArgs(typingCategory);
            alleleGroupExpander.ExpandAlleleGroup(default, default, default)
                .ReturnsForAnyArgs(new[] { alleleInGroup });

            await metadataService.GetHlaMetadata(DefaultLocus, "allele-group", "hla-db-version");

            await hlaMetadataRepository.Received(1).GetHlaMetadataRowIfExists(
                DefaultLocus,
                alleleInGroup,
                TypingMethod.Molecular,
                Arg.Any<string>());
        }

        [TestCase("99:XX", "99:XX")]
        [TestCase("*99:XX", "99:XX")]
        public async Task GetHlaMetadata_WhenXxCode_GetsMetadataForXXCode(string hlaName, string lookupName)
        {
            hlaCategorisationService.GetHlaTypingCategory(lookupName).Returns(HlaTypingCategory.XxCode);

            await metadataService.GetHlaMetadata(DefaultLocus, hlaName, "hla-db-version");

            await hlaMetadataRepository.Received().GetHlaMetadataRowIfExists(DefaultLocus, lookupName, TypingMethod.Molecular, Arg.Any<string>());
        }

        [TestCase("*AlleleName", "AlleleName")]
        [TestCase("AlleleName", "AlleleName")]
        public async Task GetHlaMetadata_WhenAllele_GetsMetadataForAllele(string hlaName, string lookupName)
        {
            hlaCategorisationService.GetHlaTypingCategory(hlaName).Returns(HlaTypingCategory.Allele);

            await metadataService.GetHlaMetadata(DefaultLocus, hlaName, "hla-db-version");

            await hlaMetadataRepository.Received().GetHlaMetadataRowIfExists(DefaultLocus, lookupName, TypingMethod.Molecular, Arg.Any<string>());
        }

        [Test]
        public async Task GetHlaMetadata_WhenSubmittedAlleleNameNotFound_GetsMetadataForCurrentName()
        {
            const string submittedAlleleName = "SUBMITTED-NAME";
            const string currentAlleleName = "CURRENT-NAME";

            hlaCategorisationService.GetHlaTypingCategory(submittedAlleleName)
                .Returns(HlaTypingCategory.Allele);

            alleleNamesMetadataService.GetCurrentAlleleNames(DefaultLocus, submittedAlleleName, Arg.Any<string>())
                .Returns(Task.FromResult((IEnumerable<string>)new[] { currentAlleleName }));

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
        public async Task GetHlaMetadata_WhenSubmittedAlleleNameIsFound_DoesNotGetMetadataForCurrentName()
        {
            const string submittedAlleleName = "SUBMITTED-NAME";
            const string currentAlleleName = "CURRENT-NAME";

            hlaCategorisationService.GetHlaTypingCategory(submittedAlleleName)
                .Returns(HlaTypingCategory.Allele);

            alleleNamesMetadataService.GetCurrentAlleleNames(DefaultLocus, submittedAlleleName, Arg.Any<string>())
                .Returns(Task.FromResult((IEnumerable<string>)new[] { currentAlleleName }));

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