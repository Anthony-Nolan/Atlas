using Atlas.Common.ApplicationInsights;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.Data.Repositories;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies.Import;
using Atlas.MatchPrediction.Test.TestHelpers.Builders;
using FluentAssertions;
using NSubstitute;
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
        private IFrequencySetMetadataExtractor metadataExtractor;
        private IFrequencySetStreamer setStreamer;
        private IFrequencyCsvReader frequenciesStreamReader;
        private IHaplotypeFrequencySetRepository setRepository;
        private IHaplotypeFrequenciesRepository frequenciesRepository;

        private IFrequencySetImporter importer;

        [SetUp]
        public void Setup()
        {
            metadataExtractor = Substitute.For<IFrequencySetMetadataExtractor>();
            setStreamer = Substitute.For<IFrequencySetStreamer>();
            frequenciesStreamReader = Substitute.For<IFrequencyCsvReader>();
            setRepository = Substitute.For<IHaplotypeFrequencySetRepository>();
            frequenciesRepository = Substitute.For<IHaplotypeFrequenciesRepository>();
            var hmdFactory = Substitute.For<IHlaMetadataDictionaryFactory>();
            var logger = Substitute.For<ILogger>();

            importer = new FrequencySetImporter(
                metadataExtractor,
                setStreamer,
                frequenciesStreamReader,
                setRepository,
                frequenciesRepository,
                hmdFactory,
                logger
            );

            var hmd = Substitute.For<IHlaMetadataDictionary>();
            hmdFactory.BuildDictionary(default).ReturnsForAnyArgs(hmd);

            metadataExtractor.GetMetadataFromFullPath(Arg.Any<string>()).Returns(new HaplotypeFrequencySetMetadata
            {
                Name = "file"
            });

            frequenciesStreamReader.GetFrequencies(Arg.Any<Stream>()).Returns(new List<HaplotypeFrequency> { new HaplotypeFrequency() });

            setRepository.AddSet(Arg.Any<HaplotypeFrequencySet>()).Returns(new HaplotypeFrequencySet { Id = 1 });

            setStreamer.GetFileContents(default).ReturnsForAnyArgs(new MemoryStream(Encoding.UTF8.GetBytes("test")));
        }

        #region ImportBlob

        [Test]
        public void ImportFromStream_FullPathIsNull_ThrowsException()
        {
            var file = new FrequencySetFile { FullPath = null, Contents = Stream.Null };
            importer.Invoking(async service => await service.ImportFromStream(file))
                .Should().Throw<Exception>();
        }

        [Test]
        public void ImportFromStream_ContentsIsNull_ThrowsException()
        {
            var file = new FrequencySetFile { FullPath = "file", Contents = null };
            importer.Invoking(async service => await service.ImportFromStream(file))
                .Should().Throw<Exception>();
        }

        [Test]
        public void ImportFromStream_EthnicityProvidedWithoutRegistry_ThrowsException()
        {
            const string ethnicity = "ethnicity";
            const string name = "name";
            var fullPath = $"/{ethnicity}/{name}";

            metadataExtractor.GetMetadataFromFullPath(fullPath).Returns(new HaplotypeFrequencySetMetadata
            {
                EthnicityCode = ethnicity,
                Name = name
            });

            var file = new FrequencySetFile { FullPath = fullPath, Contents = Stream.Null };

            importer.Invoking(async service => await service.ImportFromStream(file)).Should().Throw<Exception>();
        }

        [Test]
        public async Task ImportFromStream_ExtractsMetadataFromFullPath()
        {
            const string fullPath = "file";

            using var file = new FrequencySetFile
            {
                FullPath = fullPath,
                Contents = new MemoryStream(Encoding.UTF8.GetBytes("test"))
            };

            await importer.ImportFromStream(file);

            metadataExtractor.Received().GetMetadataFromFullPath(fullPath);
        }

        [Test]
        public async Task ImportFromStream_AddsNewInactiveSetWithMetadataAndDateTimeStamp()
        {
            const string registry = "registry";
            const string ethnicity = "ethnicity";
            const string name = "name";
            var fullPath = $"{registry}/{ethnicity}/{name}";

            metadataExtractor.GetMetadataFromFullPath(fullPath).Returns(new HaplotypeFrequencySetMetadata
            {
                RegistryCode = registry,
                EthnicityCode = ethnicity,
                Name = name
            });

            using var file = new FrequencySetFile
            {
                FullPath = fullPath,
                Contents = new MemoryStream(Encoding.UTF8.GetBytes("test"))
            };

            await importer.ImportFromStream(file);

            await setRepository.Received().AddSet(Arg.Is<HaplotypeFrequencySet>(x =>
                !x.Active &&
                x.RegistryCode == registry &&
                x.EthnicityCode == ethnicity &&
                x.Name == name &&
                x.DateTimeAdded != null));
        }

        [Test]
        public async Task ImportFromStream_StoresFrequenciesInRepository()
        {
            var frequencies = new List<HaplotypeFrequency> { new HaplotypeFrequency() };
            frequenciesStreamReader.GetFrequencies(Arg.Any<Stream>()).Returns(frequencies);

            using var file = new FrequencySetFile
            {
                FullPath = "file",
                Contents = new MemoryStream(Encoding.UTF8.GetBytes("test"))
            };

            await importer.ImportFromStream(file);

            await frequenciesRepository.Received(1)
                .AddHaplotypeFrequencies(Arg.Any<int>(), Arg.Any<IEnumerable<HaplotypeFrequency>>());
        }

        [Test]
        public void ImportFromStream_NoFrequencies_ThrowsException()
        {
            frequenciesStreamReader.GetFrequencies(Arg.Any<Stream>()).Returns(new List<HaplotypeFrequency>());

            using var file = new FrequencySetFile
            {
                FullPath = "file",
                Contents = new MemoryStream(Encoding.UTF8.GetBytes("test"))
            };

            importer.Invoking(service => service.ImportFromStream(file)).Should().Throw<Exception>();
        }

        #endregion

        #region Import

        [Test]
        public void Import_NoMetadataProvided_ThrowsException()
        {
            importer.Invoking(async service => await service.Import(null)).Should().Throw<Exception>();
        }

        [Test]
        public void Import_NoFileNameProvided_ThrowsException()
        {
            var file = HaplotypeFrequencySetMetadataBuilder.Default
                .With(x => x.Name, (string)null);

            importer.Invoking(async service => await service.Import(file)).Should().Throw<Exception>();
        }

        [Test]
        public async Task Import_StreamsFileContents()
        {
            var file = HaplotypeFrequencySetMetadataBuilder.Default.Build();

            await importer.Import(file);

            await setStreamer.Received().GetFileContents(file.Name);
        }

        [Test]
        public void Import_EthnicityProvidedWithoutRegistry_ThrowsException()
        {
            var file = HaplotypeFrequencySetMetadataBuilder.Default
                .With(x => x.RegistryCode, (string)null);

            importer.Invoking(async service => await service.Import(file)).Should().Throw<Exception>();
        }

        [Test]
        public async Task Import_AddsNewInactiveSetWithMetadataAndDateTimeStamp()
        {
            var file = HaplotypeFrequencySetMetadataBuilder.Default.Build();

            await importer.Import(file);

            await setRepository.Received().AddSet(Arg.Is<HaplotypeFrequencySet>(x =>
                !x.Active &&
                x.RegistryCode == file.RegistryCode &&
                x.EthnicityCode == file.EthnicityCode &&
                x.Name == file.Name &&
                x.DateTimeAdded != null));
        }

        [Test]
        public async Task Import_StoresFrequenciesInRepository()
        {
            var frequencies = new List<HaplotypeFrequency> { new HaplotypeFrequency() };
            frequenciesStreamReader.GetFrequencies(Arg.Any<Stream>()).Returns(frequencies);

            await importer.Import(HaplotypeFrequencySetMetadataBuilder.Default);

            await frequenciesRepository.Received(1)
                .AddHaplotypeFrequencies(Arg.Any<int>(), Arg.Any<IEnumerable<HaplotypeFrequency>>());
        }

        [Test]
        public void Import_NoFrequencies_ThrowsException()
        {
            frequenciesStreamReader.GetFrequencies(Arg.Any<Stream>()).Returns(new List<HaplotypeFrequency>());

            importer.Invoking(service => service.Import(HaplotypeFrequencySetMetadataBuilder.Default)).Should().Throw<Exception>();
        }

        #endregion
    }
}