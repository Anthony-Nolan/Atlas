using System;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using FluentAssertions;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Services.HaplotypeFrequencies
{
    public class HaplotypeFrequencySetMetaDataServiceTests
    {
        private IFrequencySetMetadataExtractor metadataExtractor;

        [SetUp]
        public void Setup()
        {
            metadataExtractor = new FrequencySetMetadataExtractor();
        }

        [TestCase(null)]
        [TestCase("")]
        public void GetMetadataFromFileName_IsNullOrEmpty_ThrowsException(string fullFileName)
        {
            metadataExtractor.Invoking(service => service.GetMetadataFromFileName(fullFileName))
                .Should().Throw<Exception>();
        }

        [TestCase("0/1/2/fileName")]
        [TestCase("/1/2/fileName")]
        public void GetMetadataFromFileName_ContainsMoreThanTwoSubfolders_ThrowsException(string fullFileName)
        {
            metadataExtractor.Invoking(service => service.GetMetadataFromFileName(fullFileName))
                .Should().Throw<Exception>();
        }

        [Test]
        public void GetMetadataFromFileName_ContainsTwoSubfolders_SetsRegistryEthnicityAndName()
        {
            const string registry = "subfolder-1";
            const string ethnicity = "subfolder-2";
            const string fileName = "fileName";
            const string fullFileName = registry + "/" + ethnicity + "/" + fileName;

            var metaData = metadataExtractor.GetMetadataFromFileName(fullFileName);

            metaData.Registry.Should().Be(registry);
            metaData.Ethnicity.Should().Be(ethnicity);
            metaData.Name.Should().Be(fileName);
        }

        [Test]
        public void GetMetadataFromFileName_ContainsOneSubfolder_SetsOnlyRegistryAndName()
        {
            const string registry = "subfolder-1";
            const string fileName = "fileName";
            const string fullFileName = registry + "/" + fileName;

            var metaData = metadataExtractor.GetMetadataFromFileName(fullFileName);

            metaData.Registry.Should().Be(registry);
            metaData.Name.Should().Be(fileName);
            metaData.Ethnicity.Should().BeNull();
        }

        [Test]
        public void GetMetadataFromFileName_ContainsNoSubfolders_SetsOnlyName()
        {
            const string fileName = "fileName";

            var metaData = metadataExtractor.GetMetadataFromFileName(fileName);

            metaData.Name.Should().Be(fileName);
            metaData.Registry.Should().BeNull();
            metaData.Ethnicity.Should().BeNull();
        }
    }
}