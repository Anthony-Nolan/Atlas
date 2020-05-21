using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.Data.Repositories;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Atlas.MatchPrediction.Test.Services.HaplotypeFrequencies
{
    public class HaplotypeFrequencySetImportServiceTests
    {
        private IHaplotypeFrequenciesStreamReader frequenciesStreamReader;
        private IHaplotypeFrequencySetRepository setRepository;
        private IHaplotypeFrequenciesRepository frequenciesRepository;

        private IHaplotypeFrequencySetImportService importService;

        [SetUp]
        public void Setup()
        {
            frequenciesStreamReader = Substitute.For<IHaplotypeFrequenciesStreamReader>();
            setRepository = Substitute.For<IHaplotypeFrequencySetRepository>();
            frequenciesRepository = Substitute.For<IHaplotypeFrequenciesRepository>();

            // have to supply a final empty list to prevent infinite loop
            frequenciesStreamReader.GetFrequencies(Arg.Any<Stream>(), Arg.Any<int>(), Arg.Any<int>())
                .Returns(new List<HaplotypeFrequency> {new HaplotypeFrequency()}, new List<HaplotypeFrequency>());

            setRepository.AddSet(Arg.Any<HaplotypeFrequencySet>())
                .Returns(new HaplotypeFrequencySet { Id = 1 });

            importService = new HaplotypeFrequencySetImportService(
                frequenciesStreamReader,
                setRepository,
                frequenciesRepository);
        }

        [Test]
        public void Import_MetaDataIsNull_ThrowsException()
        {
            importService.Invoking(async service => await service.Import(null, Stream.Null))
                .Should().Throw<Exception>();
        }

        [Test]
        public void Import_BlobStreamIsNull_ThrowsException()
        {
            importService.Invoking(async service => await service.Import(new HaplotypeFrequencySetMetaData(), null))
                .Should().Throw<Exception>();
        }

        [Test]
        public void Import_EthnicityProvidedWithoutRegistry_ThrowsException()
        {
            var metaData = new HaplotypeFrequencySetMetaData { Ethnicity = "ethnicity" };

            importService.Invoking(async service => await service.Import(metaData, Stream.Null))
                .Should().Throw<Exception>();
        }

        [TestCase("registry", "ethnicity")]
        [TestCase("registry", null)]
        [TestCase(null, null)]
        public async Task Import_ChecksForExistingActiveSetUsingMetaData(string registry, string ethnicity)
        {
            var metaData = new HaplotypeFrequencySetMetaData
            {
                Registry = registry,
                Ethnicity = ethnicity
            };

            await using (var stream = new MemoryStream(Encoding.UTF8.GetBytes("test")))
            {
                await importService.Import(metaData, stream);
            }

            await setRepository.Received().GetActiveSet(registry, ethnicity);
        }

        [Test]
        public async Task Import_DeactivatesExistingActiveSet()
        {
            const string registry = "registry";
            const string ethnicity = "ethnicity";
            const int setId = 123;

            var metaData = new HaplotypeFrequencySetMetaData
            {
                Registry = registry,
                Ethnicity = ethnicity
            };
            var existingSet = new HaplotypeFrequencySet { Id = setId };

            setRepository.GetActiveSet(registry, ethnicity)
                .Returns(existingSet);

            await using (var stream = new MemoryStream(Encoding.UTF8.GetBytes("test")))
            {
                await importService.Import(metaData, stream);
            }

            await setRepository.Received().DeactivateSet(Arg.Is<HaplotypeFrequencySet>(x => x.Id == setId));
        }

        [Test]
        public async Task Import_WhenNoExistingActiveSet_DoesNotDeactivateAnySet()
        {
            const string registry = "registry";
            const string ethnicity = "ethnicity";

            var metaData = new HaplotypeFrequencySetMetaData
            {
                Registry = registry,
                Ethnicity = ethnicity
            };

            setRepository.GetActiveSet(registry, ethnicity).ReturnsNull();

            await using (var stream = new MemoryStream(Encoding.UTF8.GetBytes("test")))
            {
                await importService.Import(metaData, stream);
            }

            await setRepository.DidNotReceive().DeactivateSet(Arg.Any<HaplotypeFrequencySet>());
        }

        [Test]
        public async Task Import_AddsNewActiveSetWithMetaDataAndDateTimeStamp()
        {
            const string registry = "registry";
            const string ethnicity = "ethnicity";
            const string name = "name";

            var metaData = new HaplotypeFrequencySetMetaData
            {
                Registry = registry,
                Ethnicity = ethnicity,
                Name = name
            };

            await using (var stream = new MemoryStream(Encoding.UTF8.GetBytes("test")))
            {
                await importService.Import(metaData, stream);
            }

            // will only test the date component, as cannot test the exact timestamp
            const string dateTimeFormat = "d";
            var today = DateTimeOffset.Now.ToString(dateTimeFormat);

            await setRepository.Received().AddSet(Arg.Is<HaplotypeFrequencySet>(x =>
                x.Active &&
                x.Registry == registry &&
                x.Ethnicity == ethnicity &&
                x.Name == name &&
                x.DateTimeAdded.ToString(dateTimeFormat).Equals(today)));
        }

        [Test]
        public async Task Import_GetsFrequenciesFromStartOfStream()
        {
            await using (var stream = new MemoryStream(Encoding.UTF8.GetBytes("test")))
            {
                await importService.Import(new HaplotypeFrequencySetMetaData(), stream);
            }

            frequenciesStreamReader.Received(1).GetFrequencies(Arg.Any<Stream>(), batchSize: Arg.Any<int>(), startFrom: Arg.Is(0));
        }

        [Test]
        public async Task Import_StoresFrequenciesInRepository()
        {
            // have to supply empty list to prevent infinite loop
            var nonEmptyList = new List<HaplotypeFrequency> { new HaplotypeFrequency() };
            var emptyList = new List<HaplotypeFrequency>();
            frequenciesStreamReader.GetFrequencies(Arg.Any<Stream>(), Arg.Any<int>(), Arg.Any<int>())
                .Returns(nonEmptyList, emptyList);

            await using (var stream = new MemoryStream(Encoding.UTF8.GetBytes("test")))
            {
                await importService.Import(new HaplotypeFrequencySetMetaData(), stream);
            }

            await frequenciesRepository.Received(1)
                .AddHaplotypeFrequencies(Arg.Any<int>(), Arg.Any<IEnumerable<HaplotypeFrequency>>());
        }

        [Test]
        public async Task Import_GetFrequenciesWithIncrementedStartFromValue()
        {
            var nonEmptyList = new List<HaplotypeFrequency>
            {
                new HaplotypeFrequency(),
                new HaplotypeFrequency()
            };
            var emptyList = new List<HaplotypeFrequency>();

            // have to supply final empty list to prevent infinite loop
            frequenciesStreamReader.GetFrequencies(Arg.Any<Stream>(), Arg.Any<int>(), Arg.Any<int>())
                .Returns(nonEmptyList, emptyList);

            await using (var stream = new MemoryStream(Encoding.UTF8.GetBytes("test")))
            {
                await importService.Import(new HaplotypeFrequencySetMetaData(), stream);
            }

            frequenciesStreamReader.Received(1).GetFrequencies(Arg.Any<Stream>(), Arg.Any<int>(), Arg.Is(nonEmptyList.Count));
        }

        [Test]
        public void Import_NoFrequencies_ThrowsException()
        {
            frequenciesStreamReader.GetFrequencies(Arg.Any<Stream>(), Arg.Any<int>(), Arg.Any<int>())
                .Returns(new List<HaplotypeFrequency>());

            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes("test")))
            {
                importService.Invoking(service => service.Import(new HaplotypeFrequencySetMetaData(), stream))
                    .Should().Throw<Exception>();
            }
        }
    }
}