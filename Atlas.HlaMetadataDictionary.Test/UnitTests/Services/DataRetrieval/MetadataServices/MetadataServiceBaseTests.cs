using System;
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
using AutoFixture;
using AwesomeAssertions;
using LazyCache;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
using NUnit.Framework;

namespace Atlas.HlaMetadataDictionary.Test.UnitTests.Services.DataRetrieval.MetadataServices
{
    /// <summary>
    /// <see cref="MetadataServiceBase{T}"/> is abstract; concrete class used to test base functionality.
    /// </summary>
    [TestFixture]
    public class MetadataServiceBaseTests
    {
        private AlleleNamesMetadataService metadataService;
        private IAlleleNamesMetadataRepository repository;
        private IHlaCategorisationService hlaCategorisationService;
        private IAppCache cache;

        private GGroupToPGroupMetadataService gGroupToPGroupService;

        private IGGroupToPGroupMetadataRepository gGroupToPGroupRepository;
        private Fixture fixture;

        [SetUp]
        public void SetUp()
        {
            fixture = new Fixture();

            repository = Substitute.For<IAlleleNamesMetadataRepository>();
            hlaCategorisationService = Substitute.For<IHlaCategorisationService>();
            gGroupToPGroupRepository = Substitute.For<IGGroupToPGroupMetadataRepository>();

            cache = AppCacheBuilder.NewDefaultCache();
            var cacheProvider = Substitute.For<IPersistentCacheProvider>();
            cacheProvider.Cache.Returns(cache);

            metadataService = new AlleleNamesMetadataService(repository, hlaCategorisationService, cacheProvider);
            gGroupToPGroupService = new GGroupToPGroupMetadataService(gGroupToPGroupRepository, cacheProvider);

            repository.GetAlleleNameIfExists(default, default, default)
                .ReturnsForAnyArgs(new AlleleNameMetadata("A*", default, default));

            hlaCategorisationService.GetHlaTypingCategory(default).ReturnsForAnyArgs(HlaTypingCategory.Allele);
        }

        [Test]
        public async Task GetMetadata_CacheDoesNotContainMetadataValue_FetchesMetadataFromRepository()
        {
            const Locus locus = Locus.A;
            const string lookupName = "hla";
            const string version = "version";

            await metadataService.GetCurrentAlleleNames(locus, lookupName, version);

            await repository.Received().GetAlleleNameIfExists(locus, lookupName, version);
        }

        [Test]
        public async Task GetMetadata_MultipleLookupsWithSameParameters_OnlyFetchesMetadataFromRepositoryOnce()
        {
            const Locus locus = Locus.A;
            const string lookupName = "hla";
            const string version = "version";

            await Task.WhenAll(
                metadataService.GetCurrentAlleleNames(locus, lookupName, version),
                metadataService.GetCurrentAlleleNames(locus, lookupName, version),
                metadataService.GetCurrentAlleleNames(locus, lookupName, version),
                metadataService.GetCurrentAlleleNames(locus, lookupName, version)
            );

            await repository.Received(1).GetAlleleNameIfExists(locus, lookupName, version);
        }

        [Test]
        public async Task GetMetadata_MultipleLookupsWithSameHlaButDifferentVersions_OnlyFetchesMetadataFromRepositoryOncePerVersion()
        {
            const Locus locus = Locus.A;
            const string lookupName = "hla";
            const string versionOne = "version-1";
            const string versionTwo = "version-2";

            await Task.WhenAll(
                metadataService.GetCurrentAlleleNames(locus, lookupName, versionOne),
                metadataService.GetCurrentAlleleNames(locus, lookupName, versionOne),
                metadataService.GetCurrentAlleleNames(locus, lookupName, versionTwo),
                metadataService.GetCurrentAlleleNames(locus, lookupName, versionTwo)
            );

            await repository.Received(1).GetAlleleNameIfExists(locus, lookupName, versionOne);
            await repository.Received(1).GetAlleleNameIfExists(locus, lookupName, versionTwo);

        }

        // ---- A name with no data is remembered; an infrastructure fault is not ------------------------

        [Test]
        public async Task GetMetadata_MultipleLookupsOfANameWithNoData_OnlyFetchesFromRepositoryOnce()
        {
            const Locus locus = Locus.A;
            const string lookupName = "hla-that-does-not-exist";
            const string version = "version";

            // The repository is an *IfExists method and answers null; PerformLookup turns that into InvalidHlaException.
            repository.GetAlleleNameIfExists(locus, lookupName, version).ReturnsNull();

            for (var i = 0; i < 5; i++)
            {
                await ShouldFailLookup(locus, lookupName, version);
            }

            await repository.Received(1).GetAlleleNameIfExists(locus, lookupName, version);
        }

        [Test]
        public async Task GetMetadata_WhenTheNameHasNoData_StillReportsTheFailureOnEveryCall()
        {
            const Locus locus = Locus.A;
            const string lookupName = "hla-that-does-not-exist";
            const string version = "version";

            repository.GetAlleleNameIfExists(locus, lookupName, version).ReturnsNull();

            // Caching the outcome must not turn a failed lookup into a successful one: every caller still sees the
            // same exception, because DonorHlaExpander and SearchRunner use it as their expected-error pathway.
            var first = await ShouldFailLookup(locus, lookupName, version);
            var second = await ShouldFailLookup(locus, lookupName, version);

            first.Message.Should().Be(second.Message);
        }

        [Test]
        public async Task GetMetadata_WhenTheRepositoryFaults_DoesNotCacheTheFault()
        {
            const Locus locus = Locus.A;
            const string lookupName = "hla";
            const string version = "version";

            var faulted = true;

            // A transient infrastructure fault - a failed storage request, a timeout - rather than a missing name.
            // Caching it for the cache lifetime would turn a blip into an outage, so the entry must not be kept.
            repository.GetAlleleNameIfExists(locus, lookupName, version).Returns<IAlleleNameMetadata>(_ =>
                faulted
                    ? throw new TimeoutException("storage is having a moment")
                    : new AlleleNameMetadata("A*", default, default));

            await ShouldFailLookup(locus, lookupName, version);

            faulted = false;
            var recovered = await metadataService.GetCurrentAlleleNames(locus, lookupName, version);

            recovered.Should().NotBeNull();
            await repository.Received(2).GetAlleleNameIfExists(locus, lookupName, version);
        }

        // ---- The same lookup, with "no data for this name" as a value rather than an exception ---

        [Test]
        public async Task TryGetMetadata_WhenTheNameHasNoData_ReportsNotFoundWithoutThrowing()
        {
            var locus = fixture.Create<Locus>();
            var gGroup = fixture.Create<string>();
            var version = fixture.Create<string>();

            gGroupToPGroupRepository.GetPGroupByGGroupIfExists(locus, gGroup, version).ReturnsNull();

            var (wasFound, pGroup) = await gGroupToPGroupService.TryConvertGGroupToPGroup(locus, gGroup, version);

            wasFound.Should().BeFalse();
            pGroup.Should().BeNull();
        }

        [Test]
        public async Task TryGetMetadata_WhenTheNameIsInTheData_ReportsTheValue()
        {
            var locus = fixture.Create<Locus>();
            var gGroup = fixture.Create<string>();
            var version = fixture.Create<string>();
            var expectedPGroup = fixture.Create<string>();

            gGroupToPGroupRepository.GetPGroupByGGroupIfExists(locus, gGroup, version)
                .Returns(new MolecularTypingToPGroupMetadata(locus, gGroup, expectedPGroup));

            var (wasFound, pGroup) = await gGroupToPGroupService.TryConvertGGroupToPGroup(locus, gGroup, version);

            wasFound.Should().BeTrue();
            pGroup.Should().Be(expectedPGroup);
        }

        [Test]
        public async Task TryGetMetadata_WhenTheNameIsInTheDataButHasNoPGroup_ReportsFoundWithANullValue()
        {
            var locus = fixture.Create<Locus>();
            var gGroup = fixture.Create<string>();
            var version = fixture.Create<string>();

            // The outcome a bare value cannot express, and the reason the tuple carries a bool at all: a G group of
            // null-expressing alleles IS in the data and has no P group. Reading that as "not found" would log a
            // conversion failure and drop the haplotype, instead of pooling it under a null P group.
            gGroupToPGroupRepository.GetPGroupByGGroupIfExists(locus, gGroup, version)
                .Returns(new MolecularTypingToPGroupMetadata(locus, gGroup, null));

            var (wasFound, pGroup) = await gGroupToPGroupService.TryConvertGGroupToPGroup(locus, gGroup, version);

            wasFound.Should().BeTrue();
            pGroup.Should().BeNull();
        }

        [Test]
        public async Task TryGetMetadata_MultipleLookupsOfANameWithNoData_OnlyFetchesFromRepositoryOnce()
        {
            var locus = fixture.Create<Locus>();
            var gGroup = fixture.Create<string>();
            var version = fixture.Create<string>();

            gGroupToPGroupRepository.GetPGroupByGGroupIfExists(locus, gGroup, version).ReturnsNull();

            for (var i = 0; i < 5; i++)
            {
                var (wasFound, _) = await gGroupToPGroupService.TryConvertGGroupToPGroup(locus, gGroup, version);
                wasFound.Should().BeFalse();
            }

            await gGroupToPGroupRepository.Received(1).GetPGroupByGGroupIfExists(locus, gGroup, version);
        }

        [Test]
        public async Task TryGetMetadata_AfterTheThrowingPathFailedForTheName_ReportsNotFoundFromTheSameCachedOutcome()
        {
            var locus = fixture.Create<Locus>();
            var gGroup = fixture.Create<string>();
            var version = fixture.Create<string>();

            gGroupToPGroupRepository.GetPGroupByGGroupIfExists(locus, gGroup, version).ReturnsNull();

            // One memo, read two ways: whichever path meets the name first pays for the lookup, and the other is served
            // from the cache. The two must agree, or the answer would depend on which caller arrived first.
            await gGroupToPGroupService.Invoking(s => s.ConvertGGroupToPGroup(locus, gGroup, version))
                .Should().ThrowAsync<HlaMetadataDictionaryException>();

            var (wasFound, _) = await gGroupToPGroupService.TryConvertGGroupToPGroup(locus, gGroup, version);

            wasFound.Should().BeFalse();
            await gGroupToPGroupRepository.Received(1).GetPGroupByGGroupIfExists(locus, gGroup, version);
        }

        [Test]
        public async Task TryGetMetadata_WhenTheRepositoryFaults_PropagatesTheFaultAndDoesNotCacheIt()
        {
            var locus = fixture.Create<Locus>();
            var gGroup = fixture.Create<string>();
            var version = fixture.Create<string>();
            var expectedPGroup = fixture.Create<string>();

            var faulted = true;

            // This is the whole point of having a non-throwing method as well as a throwing one: "no data for this
            // name" is an answer, and a failed storage request is not. Reporting the second as the first would predict
            // from an incomplete expansion, silently and with nothing logged as an error.
            gGroupToPGroupRepository.GetPGroupByGGroupIfExists(locus, gGroup, version)
                .Returns<IMolecularTypingToPGroupMetadata>(_ =>
                    faulted
                        ? throw new TimeoutException("storage is having a moment")
                        : new MolecularTypingToPGroupMetadata(locus, gGroup, expectedPGroup));

            await gGroupToPGroupService.Invoking(s => s.TryConvertGGroupToPGroup(locus, gGroup, version))
                .Should().ThrowAsync<TimeoutException>();

            faulted = false;
            var (wasFound, pGroup) = await gGroupToPGroupService.TryConvertGGroupToPGroup(locus, gGroup, version);

            wasFound.Should().BeTrue();
            pGroup.Should().Be(expectedPGroup);
            await gGroupToPGroupRepository.Received(2).GetPGroupByGGroupIfExists(locus, gGroup, version);
        }

        private async Task<HlaMetadataDictionaryException> ShouldFailLookup(Locus locus, string lookupName, string version)
        {
            try
            {
                await metadataService.GetCurrentAlleleNames(locus, lookupName, version);
            }
            catch (HlaMetadataDictionaryException exception)
            {
                return exception;
            }

            throw new AssertionException($"Expected a lookup of '{lookupName}' to fail, but it succeeded.");
        }
    }
}
