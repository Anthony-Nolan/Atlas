﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.Data.Repositories;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies.Import;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Services.HaplotypeFrequencies
{
    public class HaplotypeFrequencySetImportServiceTests
    {
        private IFrequencySetMetadataExtractor metadataExtractor;
        private IFrequencyCsvReader frequenciesStreamReader;
        private IHaplotypeFrequencySetRepository setRepository;
        private IHaplotypeFrequenciesRepository frequenciesRepository;

        private IFrequencySetImporter importer;

        [SetUp]
        public void Setup()
        {
            metadataExtractor = Substitute.For<IFrequencySetMetadataExtractor>();
            frequenciesStreamReader = Substitute.For<IFrequencyCsvReader>();
            setRepository = Substitute.For<IHaplotypeFrequencySetRepository>();
            frequenciesRepository = Substitute.For<IHaplotypeFrequenciesRepository>();

            metadataExtractor.GetMetadataFromFullPath(Arg.Any<string>()).Returns(new HaplotypeFrequencySetMetadata
            {
                Name = "file"
            });

            frequenciesStreamReader.GetFrequencies(Arg.Any<Stream>()).Returns(new List<HaplotypeFrequency> { new HaplotypeFrequency() });

            setRepository.AddSet(Arg.Any<HaplotypeFrequencySet>()).Returns(new HaplotypeFrequencySet { Id = 1 });

            importer = new FrequencySetImporter(
                metadataExtractor,
                frequenciesStreamReader,
                setRepository,
                frequenciesRepository);
        }

        [Test]
        public void Import_FullPathIsNull_ThrowsException()
        {
            var file = new FrequencySetFile { FullPath = null, Contents = Stream.Null };
            importer.Invoking(async service => await service.Import(file))
                .Should().Throw<Exception>();
        }

        [Test]
        public void Import_ContentsIsNull_ThrowsException()
        {
            var file = new FrequencySetFile { FullPath = "file", Contents = null };
            importer.Invoking(async service => await service.Import(file))
                .Should().Throw<Exception>();
        }

        [Test]
        public void Import_EthnicityProvidedWithoutRegistry_ThrowsException()
        {
            const string ethnicity = "ethnicity";
            const string name = "name";
            var fullPath = $"/{ethnicity}/{name}";

            metadataExtractor.GetMetadataFromFullPath(fullPath).Returns(new HaplotypeFrequencySetMetadata
            {
                Ethnicity = ethnicity,
                Name = name
            });

            var file = new FrequencySetFile { FullPath = fullPath, Contents = Stream.Null };

            importer.Invoking(async service => await service.Import(file)).Should().Throw<Exception>();
        }

        [Test]
        public async Task Import_ExtractsMetadataFromFullPath()
        {
            const string fullPath = "file";

            using var file = new FrequencySetFile
            {
                FullPath = fullPath,
                Contents = new MemoryStream(Encoding.UTF8.GetBytes("test"))
            };

            await importer.Import(file);

            metadataExtractor.Received().GetMetadataFromFullPath(fullPath);
        }

        [Test]
        public async Task Import_AddsNewInactiveSetWithMetadataAndDateTimeStamp()
        {
            const string registry = "registry";
            const string ethnicity = "ethnicity";
            const string name = "name";
            var fullPath = $"{registry}/{ethnicity}/{name}";

            metadataExtractor.GetMetadataFromFullPath(fullPath).Returns(new HaplotypeFrequencySetMetadata
            {
                Registry = registry,
                Ethnicity = ethnicity,
                Name = name
            });

            using var file = new FrequencySetFile
            {
                FullPath = fullPath,
                Contents = new MemoryStream(Encoding.UTF8.GetBytes("test"))
            };

            await importer.Import(file);

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
            var frequencies = new List<HaplotypeFrequency> { new HaplotypeFrequency() };
            frequenciesStreamReader.GetFrequencies(Arg.Any<Stream>()).Returns(frequencies);

            using var file = new FrequencySetFile
            {
                FullPath = "file",
                Contents = new MemoryStream(Encoding.UTF8.GetBytes("test"))
            };

            await importer.Import(file);

            await frequenciesRepository.Received(1)
                .AddHaplotypeFrequencies(Arg.Any<int>(), Arg.Any<IEnumerable<HaplotypeFrequency>>());
        }

        [Test]
        public void Import_NoFrequencies_ThrowsException()
        {
            frequenciesStreamReader.GetFrequencies(Arg.Any<Stream>()).Returns(new List<HaplotypeFrequency>());

            using var file = new FrequencySetFile
            {
                FullPath = "file",
                Contents = new MemoryStream(Encoding.UTF8.GetBytes("test"))
            };

            importer.Invoking(service => service.Import(file)).Should().Throw<Exception>();
        }
    }
}