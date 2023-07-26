using Atlas.Common.ApplicationInsights;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.MatchPrediction.Data.Repositories;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Models.FileSchema;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies.Import;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Atlas.MatchPrediction.Test.Services.HaplotypeFrequencies.Import
{
    [TestFixture]
    public class FrequencySetImporterTests
    {
        private IFrequencyFileParser frequencyFileParser;
        private IHaplotypeFrequencySetRepository setRepository;
        private IHaplotypeFrequenciesRepository frequenciesRepository;
        private IHlaMetadataDictionaryFactory hlaMetadataDictionaryFactory;
        private IFrequencySetValidator frequencySetValidator;
        private ILogger logger;

        private IFrequencySetImporter frequencySetImporter;

        [SetUp]
        public void SetUp()
        {
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
            hlaMetadataDictionaryFactory = Substitute.For<IHlaMetadataDictionaryFactory>();
            frequencySetValidator = Substitute.For<IFrequencySetValidator>();
            logger = Substitute.For<ILogger>();

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
            frequencySetImporter.Invoking(i => i.Import(file, importBehavior)).Should().Throw<Exception>();

            // Assert
            await setRepository.Received(1).ActivateSet(Arg.Any<int>());
            await frequenciesRepository.Received(1).RemoveHaplotypeFrequencies(Arg.Any<int>());
        }
    }
}
