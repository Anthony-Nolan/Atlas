using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.Caching;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.Common.GeneticData.Hla.Services;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.HlaMetadataDictionary.ExternalInterface.Exceptions;
using Atlas.HlaMetadataDictionary.InternalModels.Metadata;
using Atlas.HlaMetadataDictionary.Repositories.MetadataRepositories;
using Atlas.HlaMetadataDictionary.Services.DataRetrieval;
using LazyCache;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.HlaMetadataDictionary.Test.UnitTests.Services.AlleleNames
{
    [TestFixture]
    public class AlleleNamesMetadataServiceTests
    {
        private IAlleleNamesMetadataService metadataService;
        private IAlleleNamesMetadataRepository metadataRepository;
        private IHlaCategorisationService hlaCategorisationService;
        private IAppCache cache;
        private const Locus MatchedLocus = Locus.A;

        [SetUp]
        public void Setup()
        {
            metadataRepository = Substitute.For<IAlleleNamesMetadataRepository>();
            hlaCategorisationService = Substitute.For<IHlaCategorisationService>();

            cache = AppCacheBuilder.NewDefaultCache();
            var cacheProvider = Substitute.For<IPersistentCacheProvider>();
            cacheProvider.Cache.Returns(cache);

            metadataService = new AlleleNamesMetadataService(metadataRepository, hlaCategorisationService, cacheProvider);
            
            metadataRepository
                .GetAlleleNameIfExists(MatchedLocus, Arg.Any<string>(), Arg.Any<string>())
                .Returns(new AlleleNameMetadata(MatchedLocus, "FAKE-ALLELE-TO-PREVENT-INVALID-HLA-EXCEPTION", new List<string>()));
        }
        
        [TestCase(null)]
        [TestCase("")]
        public void GetCurrentAlleleNames_WhenStringNullOrEmpty_ThrowsException(string nullOrEmptyString)
        {
            Assert.ThrowsAsync<HlaMetadataDictionaryException>(
                async () => await metadataService.GetCurrentAlleleNames(MatchedLocus, nullOrEmptyString, "hla-db-version"));
        }

        [Test]
        public void GetCurrentAlleleNames_WhenNotAlleleTyping_ThrowsException()
        {
            const string notAlleleName = "NOT-AN-ALLELE";
            const HlaTypingCategory notAlleleTypingCategory = HlaTypingCategory.Serology;

            hlaCategorisationService.GetHlaTypingCategory(notAlleleName).Returns(notAlleleTypingCategory);

            Assert.ThrowsAsync<HlaMetadataDictionaryException>(
                async () => await metadataService.GetCurrentAlleleNames(MatchedLocus, notAlleleName, "hla-db-version"));
        }

        [Test]
        public async Task GetCurrentAlleleNames_WhenAlleleTyping_LooksUpAlleleName()
        {
            const string lookupName = "allele";

            hlaCategorisationService.GetHlaTypingCategory(Arg.Any<string>()).Returns(HlaTypingCategory.Allele);

            const string hlaNomenclatureVersion = "3333";
            await metadataService.GetCurrentAlleleNames(MatchedLocus, lookupName, hlaNomenclatureVersion);

            await metadataRepository.Received().GetAlleleNameIfExists(MatchedLocus, lookupName, hlaNomenclatureVersion);
        }

        [Test]
        public async Task GetCurrentAlleleNames_WhenAlleleTyping_TrimsAlleleNameBeforeLookUp()
        {
            const string submittedLookupName = "*name";
            const string trimmedLookupName = "name";

            hlaCategorisationService.GetHlaTypingCategory(Arg.Any<string>()).Returns(HlaTypingCategory.Allele);

            const string hlaNomenclatureVersion = "3333";
            await metadataService.GetCurrentAlleleNames(MatchedLocus, submittedLookupName, hlaNomenclatureVersion);

            await metadataRepository.Received().GetAlleleNameIfExists(MatchedLocus, trimmedLookupName, hlaNomenclatureVersion);
        }
    }
}
