using Atlas.Common.ApplicationInsights;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.Data.Repositories;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Models.FileSchema;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies.Import;
using AutoFixture;
using AwesomeAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NSubstitute.Extensions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Atlas.MatchPrediction.Test.Services.HaplotypeFrequencies.Import
{
    [TestFixture]
    public class FrequencySetImporterTests
    {
        private Fixture fixture;

        private IFrequencyFileParser frequencyFileParser;
        private IHaplotypeFrequencySetRepository setRepository;
        private IHaplotypeFrequenciesRepository frequenciesRepository;
        private IHlaMetadataDictionaryFactory hlaMetadataDictionaryFactory;
        private IFrequencySetValidator frequencySetValidator;
        private IAtlasLogger logger;

        private IFrequencySetImporter frequencySetImporter;

        [SetUp]
        public void SetUp()
        {
            fixture = new Fixture();

            frequencyFileParser = Substitute.For<IFrequencyFileParser>();
            frequencyFileParser.GetFrequencies(default).ReturnsForAnyArgs(new FrequencySetFileSchema
            {
                TypingCategory = ImportTypingCategory.LargeGGroup,
                Frequencies = new List<FrequencyRecord> { new FrequencyRecord() }
            });

            setRepository = Substitute.For<IHaplotypeFrequencySetRepository>();
            setRepository.AddSet(default).ReturnsForAnyArgs(new Data.Models.HaplotypeFrequencySet { Id = 1 });
            setRepository.ActivateSet(default).ThrowsForAnyArgs(new Exception());

            frequenciesRepository = Substitute.For<IHaplotypeFrequenciesRepository>();
            // The import reads back the rows of one of its sets, for the content hash of its sets.
            frequenciesRepository.GetAllHaplotypeFrequencies(default).ReturnsForAnyArgs(new List<LightweightHaplotypeFrequencyRecord>());

            hlaMetadataDictionaryFactory = Substitute.For<IHlaMetadataDictionaryFactory>();
            frequencySetValidator = Substitute.For<IFrequencySetValidator>();
            logger = Substitute.For<IAtlasLogger>();

            frequencySetImporter = new FrequencySetImporter(frequencyFileParser, setRepository, frequenciesRepository, hlaMetadataDictionaryFactory, frequencySetValidator, logger);
        }

        [Test]
        public async Task Import_ExceptionDuringFrequencySetActivation_ShouldRemoveInsertedHaplotypeFrequencies()
        {
            // Arrange
            var file = new FrequencySetFile
            {
                Contents = new System.IO.MemoryStream()
            };

            var importBehavior = new FrequencySetImportBehaviour
            {
                ShouldBypassHlaValidation = true
            };

            // Act
            await frequencySetImporter.Invoking(i => i.Import(file, importBehavior)).Should().ThrowAsync<Exception>();

            // Assert
            await setRepository.Received(1).ActivateSet(Arg.Any<int>());
            await frequenciesRepository.Received(1).RemoveHaplotypeFrequencies(Arg.Any<int>());
        }

        /// <summary>All the sets of an import get the same rows, so one read is enough for all their hashes.</summary>
        [Test]
        public async Task Import_WritesTheContentHashOfTheRowsReadBackToAllItsSets_BeforeItActivatesThem()
        {
            frequencyFileParser.GetFrequencies(default).ReturnsForAnyArgs(new FrequencySetFileSchema
            {
                TypingCategory = ImportTypingCategory.LargeGGroup,
                RegistryCodes = fixture.CreateMany<string>(2).ToArray(),
                Frequencies = new List<FrequencyRecord> { new FrequencyRecord() }
            });
            var setIds = fixture.CreateMany<int>(2).ToList();
            setRepository.AddSet(default).ReturnsForAnyArgs(
                new Data.Models.HaplotypeFrequencySet { Id = setIds[0] },
                new Data.Models.HaplotypeFrequencySet { Id = setIds[1] });
            // Configure(): the activation of SetUp throws, and a plain call here would throw it.
            setRepository.Configure().ActivateSet(default).ReturnsForAnyArgs(Task.CompletedTask);
            var rowsReadBack = fixture.CreateMany<LightweightHaplotypeFrequencyRecord>().ToList();
            frequenciesRepository.GetAllHaplotypeFrequencies(setIds[0]).Returns(rowsReadBack);
            var expectedHash = FrequencySetContentHash.Calculate(rowsReadBack);

            await frequencySetImporter.Import(
                new FrequencySetFile { Contents = new MemoryStream() },
                new FrequencySetImportBehaviour { ShouldBypassHlaValidation = true, ShouldConvertLargeGGroupsToPGroups = false });

            Received.InOrder(() =>
            {
                setRepository.SetContentHash(
                    Arg.Is<IReadOnlyCollection<int>>(ids => ids.SequenceEqual(setIds)),
                    Arg.Is<byte[]>(hash => hash.SequenceEqual(expectedHash)));
                setRepository.ActivateSet(setIds[0]);
                setRepository.ActivateSet(setIds[1]);
            });
            await frequenciesRepository.ReceivedWithAnyArgs(1).GetAllHaplotypeFrequencies(default);
        }
    }
}
