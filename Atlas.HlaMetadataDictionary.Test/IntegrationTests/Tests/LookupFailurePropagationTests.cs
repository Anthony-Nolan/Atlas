using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Utils.Extensions;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.HlaMetadataDictionary.ExternalInterface.Exceptions;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata;
using Atlas.HlaMetadataDictionary.InternalModels.Metadata;
using Atlas.HlaMetadataDictionary.InternalModels.MetadataTableRows;
using Atlas.HlaMetadataDictionary.Repositories.MetadataRepositories;
using Atlas.HlaMetadataDictionary.Services.DataRetrieval;
using Atlas.HlaMetadataDictionary.Test.IntegrationTests.DependencyInjection;
using Atlas.HlaMetadataDictionary.Test.IntegrationTests.TestHelpers.FileBackedStorageStubs;
using Atlas.MultipleAlleleCodeDictionary.Settings;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Atlas.HlaMetadataDictionary.Test.IntegrationTests.Tests
{
    /// <summary>
    /// Does a failure below the dictionary reach the top of it as the failure it was?
    /// </summary>
    /// <remarks>
    /// <para>
    /// Nothing here is a mock of a class under test. The whole graph is real and wired by the real DI registration -
    /// <c>HlaScoringMetadataService</c>, <c>SearchRelatedMetadataServiceBase</c>, <c>AlleleNamesLookupBase</c>,
    /// <c>AlleleNamesMetadataService</c>, <c>MetadataServiceBase</c>, the caching, the external interface - and only
    /// the storage layer underneath it is a stub, as it is for every test in this folder. One repository is given the
    /// ability to fault, and the question asked at the top is what came out.
    /// </para>
    /// <para>
    /// Which is a question the unit tests structurally cannot ask. They exercise leaf services, whose
    /// <c>PerformLookup</c> reaches a repository and nothing else. The bug this fixture pins lived in the gap between
    /// two <c>MetadataServiceBase</c> instances: the scoring lookup for an allele it does not hold falls through to
    /// <see cref="IAlleleNamesMetadataService"/>, whose own <c>GetMetadata</c> used to re-label a storage failure as
    /// an <see cref="HlaMetadataDictionaryException"/> on the way back up. The outer lookup then read that as "this
    /// name is not in the data", cached it for the lifetime of the persistent cache, and every caller above -
    /// matching skipping the donor, Match Prediction predicting from an incomplete expansion - believed it.
    /// </para>
    /// </remarks>
    [TestFixture]
    internal class LookupFailurePropagationTests
    {
        private const Locus DefaultLocus = Locus.A;
        private const string HlaVersion = FileBackedHlaMetadataRepositoryBaseReader.OlderTestHlaVersion;

        /// <summary>
        /// Well formed, and deliberately not in the test data, so that the scoring lookup falls through to the allele
        /// NAMES lookup - the nested <c>MetadataServiceBase</c> call this fixture exists to cover.
        /// </summary>
        private const string AlleleNotInTheDictionary = "9999:9999";

        private FaultInjectingAlleleNamesRepository alleleNamesRepository;
        private IHlaScoringMetadataService scoringMetadataService;
        private IHlaMetadataDictionary hlaMetadataDictionary;

        [SetUp]
        public void SetUp()
        {
            // A provider per test, not the shared one: the persistent cache is a singleton, and half of what these
            // tests assert is what does and does not end up in it.
            var services = new ServiceCollection();
            services.RegisterFileBasedHlaMetadataDictionaryForTesting(
                _ => new ApplicationInsightsSettings { LogLevel = "Info" },
                DependencyInjectionUtils.OptionsReaderFor<MacDictionarySettings>()
            );

            alleleNamesRepository = new FaultInjectingAlleleNamesRepository();
            services.AddSingleton<IAlleleNamesMetadataRepository>(alleleNamesRepository);

            var provider = services.BuildServiceProvider();
            scoringMetadataService = provider.GetRequiredService<IHlaScoringMetadataService>();
            hlaMetadataDictionary = provider.GetRequiredService<IHlaMetadataDictionaryFactory>().BuildDictionary(HlaVersion);
        }

        [Test]
        public async Task GetHlaMetadata_WhenANestedLookupFaults_PropagatesTheFaultAsItself()
        {
            alleleNamesRepository.Fault = new TimeoutException("storage is having a moment");

            // Not an HlaMetadataDictionaryException. That is the entire point: a caller has to be able to tell a
            // storage account having a bad minute from an HLA name that does not exist.
            await scoringMetadataService.Invoking(s => s.GetHlaMetadata(DefaultLocus, AlleleNotInTheDictionary, HlaVersion))
                .Should().ThrowAsync<TimeoutException>();
        }

        [Test]
        public async Task TryConvertHla_WhenANestedLookupFaults_PropagatesInsteadOfReportingNotFound()
        {
            alleleNamesRepository.Fault = new TimeoutException("storage is having a moment");

            // The non-throwing route, which Match Prediction uses and which has no catch of its own. Reporting this
            // as `(false, null)` is what would let a prediction run on an incomplete expansion with nothing logged
            // as an error.
            await hlaMetadataDictionary
                .Invoking(d => d.TryConvertHla(DefaultLocus, AlleleNotInTheDictionary, TargetHlaCategory.PGroup))
                .Should().ThrowAsync<TimeoutException>();
        }

        [Test]
        public async Task GetHlaMetadata_WhenANestedLookupFaults_DoesNotCacheTheFault()
        {
            alleleNamesRepository.Fault = new TimeoutException("storage is having a moment");

            await scoringMetadataService.Invoking(s => s.GetHlaMetadata(DefaultLocus, AlleleNotInTheDictionary, HlaVersion))
                .Should().ThrowAsync<TimeoutException>();

            // A transient fault must not be remembered as an answer. While it was re-labelled as a missing name it
            // was cached like one, so a blip lasted as long as the persistent cache did - a day, by default.
            alleleNamesRepository.Fault = null;
            var callsAfterTheFault = alleleNamesRepository.CallCount;

            await scoringMetadataService.Invoking(s => s.GetHlaMetadata(DefaultLocus, AlleleNotInTheDictionary, HlaVersion))
                .Should().ThrowAsync<HlaMetadataDictionaryException>();

            alleleNamesRepository.CallCount.Should().BeGreaterThan(callsAfterTheFault);
        }

        [Test]
        public async Task GetHlaMetadata_WhenTheNameIsNotInTheData_StillReportsAMissingName()
        {
            // The control, and the reason narrowing the catch is safe rather than merely correct: a genuine miss is
            // untouched. DonorHlaExpander, SearchRunner and RepeatSearchRunner all treat this exception as an
            // expected error, and all three still get it.
            await scoringMetadataService.Invoking(s => s.GetHlaMetadata(DefaultLocus, AlleleNotInTheDictionary, HlaVersion))
                .Should().ThrowAsync<HlaMetadataDictionaryException>();
        }

        [Test]
        public async Task TryConvertHla_WhenTheNameIsNotInTheData_ReportsNotFound()
        {
            var (wasFound, hla) = await hlaMetadataDictionary.TryConvertHla(
                DefaultLocus, AlleleNotInTheDictionary, TargetHlaCategory.PGroup
            );

            wasFound.Should().BeFalse();
            hla.Should().BeNull();
        }

        [Test]
        public async Task GetHlaMetadata_WhenTheNameIsUnparseable_ReportsAMissingNameRatherThanFaulting()
        {
            // The categoriser reports a name it cannot parse by throwing AtlasHttpException, which is not a lookup
            // failure in this dictionary's vocabulary. One malformed donor record used to be re-labelled along with
            // everything else; now that only genuine misses are, it has to say so itself - or a single bad record
            // would fail an entire data refresh batch.
            await scoringMetadataService.Invoking(s => s.GetHlaMetadata(DefaultLocus, "not-an-hla-name", HlaVersion))
                .Should().ThrowAsync<HlaMetadataDictionaryException>();
        }

        /// <summary>
        /// The real file-backed repository, with a switch on it. The only stubbed thing in the graph, and it is
        /// stubbed at the layer every test in this folder already stubs: storage.
        /// </summary>
        private class FaultInjectingAlleleNamesRepository : IAlleleNamesMetadataRepository
        {
            private readonly FileBackedAlleleNamesMetadataRepository inner = new();

            public Exception Fault { get; set; }

            public int CallCount { get; private set; }

            public Task<IAlleleNameMetadata> GetAlleleNameIfExists(Locus locus, string lookupName, string hlaNomenclatureVersion)
            {
                CallCount++;

                return Fault != null
                    ? throw Fault
                    : inner.GetAlleleNameIfExists(locus, lookupName, hlaNomenclatureVersion);
            }

            public Task LoadDataIntoMemory(string hlaNomenclatureVersion) => inner.LoadDataIntoMemory(hlaNomenclatureVersion);

            public Task RecreateHlaMetadataTable(IEnumerable<ISerialisableHlaMetadata> metadata, string hlaNomenclatureVersion) =>
                inner.RecreateHlaMetadataTable(metadata, hlaNomenclatureVersion);

            public Task<HlaMetadataTableRow> GetHlaMetadataRowIfExists(
                Locus locus,
                string lookupName,
                TypingMethod typingMethod,
                string hlaNomenclatureVersion) =>
                inner.GetHlaMetadataRowIfExists(locus, lookupName, typingMethod, hlaNomenclatureVersion);
        }
    }
}