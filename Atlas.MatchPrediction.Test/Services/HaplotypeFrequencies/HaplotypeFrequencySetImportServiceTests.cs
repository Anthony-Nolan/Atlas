using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.Data.Repositories;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Services.HaplotypeFrequencies
{
    public class HaplotypeFrequencySetImportServiceTests
    {
        private IFrequencyCsvReader frequenciesStreamReader;
        private IHaplotypeFrequencySetRepository setRepository;
        private IHaplotypeFrequenciesRepository frequenciesRepository;

        private IFrequencySetImporter importer;

        [SetUp]
        public void Setup()
        {
            frequenciesStreamReader = Substitute.For<IFrequencyCsvReader>();
            setRepository = Substitute.For<IHaplotypeFrequencySetRepository>();
            frequenciesRepository = Substitute.For<IHaplotypeFrequenciesRepository>();

            frequenciesStreamReader.GetFrequencies(Arg.Any<Stream>()).Returns(new List<HaplotypeFrequency> {new HaplotypeFrequency()});

            setRepository.AddSet(Arg.Any<HaplotypeFrequencySet>()).Returns(new HaplotypeFrequencySet {Id = 1});

            importer = new FrequencySetImporter(
                frequenciesStreamReader,
                setRepository,
                frequenciesRepository);
        }

        [Test]
        public void Import_MetaDataIsNull_ThrowsException()
        {
            importer.Invoking(async service => await service.Import(null, Stream.Null))
                .Should().Throw<Exception>();
        }

        [Test]
        public void Import_BlobStreamIsNull_ThrowsException()
        {
            importer.Invoking(async service => await service.Import(new HaplotypeFrequencySetMetadata(), null))
                .Should().Throw<Exception>();
        }

        [Test]
        public void Import_EthnicityProvidedWithoutRegistry_ThrowsException()
        {
            var metaData = new HaplotypeFrequencySetMetadata {Ethnicity = "ethnicity"};

            importer.Invoking(async service => await service.Import(metaData, Stream.Null))
                .Should().Throw<Exception>();
        }

        [Test]
        public async Task Import_AddsNewInactiveSetWithMetadataAndDateTimeStamp()
        {
            const string registry = "registry";
            const string ethnicity = "ethnicity";
            const string name = "name";

            var metaData = new HaplotypeFrequencySetMetadata
            {
                Registry = registry,
                Ethnicity = ethnicity,
                Name = name
            };

            await using (var stream = new MemoryStream(Encoding.UTF8.GetBytes("test")))
            {
                await importer.Import(metaData, stream);
            }

            await setRepository.Received().AddSet(Arg.Is<HaplotypeFrequencySet>(x =>
                !x.Active &&
                x.RegistryCode == registry &&
                x.EthnicityCode == ethnicity &&
                x.Name == name &&
                x.DateTimeAdded != null));
        }

        [Test]
        public async Task Import_StoresFrequenciesInRepository()
        {
            var frequencies = new List<HaplotypeFrequency> {new HaplotypeFrequency()};
            frequenciesStreamReader.GetFrequencies(Arg.Any<Stream>()).Returns(frequencies);

            await using (var stream = new MemoryStream(Encoding.UTF8.GetBytes("test")))
            {
                await importer.Import(new HaplotypeFrequencySetMetadata(), stream);
            }

            await frequenciesRepository.Received(1)
                .AddHaplotypeFrequencies(Arg.Any<int>(), Arg.Any<IEnumerable<HaplotypeFrequency>>());
        }

        [Test]
        public void Import_NoFrequencies_ThrowsException()
        {
            frequenciesStreamReader.GetFrequencies(Arg.Any<Stream>()).Returns(new List<HaplotypeFrequency>());

            importer.Invoking(service =>
            {
                using var stream = new MemoryStream(Encoding.UTF8.GetBytes("test"));
                return service.Import(new HaplotypeFrequencySetMetadata(), stream);
            }).Should().Throw<Exception>();
        }
    }
}