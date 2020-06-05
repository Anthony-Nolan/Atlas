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
        public void GetMetadataFromFullPath_IsNullOrEmpty_ThrowsException(string fullPath)
        {
            metadataExtractor.Invoking(service => service.GetMetadataFromFullPath(fullPath))
                .Should().Throw<Exception>();
        }

        [TestCase("0/1/2/fileName")]
        [TestCase("/1/2/fileName")]
        public void GetMetadataFromFullPath_ContainsMoreThanTwoSubfolders_ThrowsException(string fullPath)
        {
            metadataExtractor.Invoking(service => service.GetMetadataFromFullPath(fullPath))
                .Should().Throw<Exception>();
        }

        [Test]
        public void GetMetadataFromFullPath_ContainsTwoSubfolders_SetsRegistryEthnicityAndName()
        {
            const string registry = "subfolder-1";
            const string ethnicity = "subfolder-2";
            const string fileName = "fileName";
            const string fullPath = registry + "/" + ethnicity + "/" + fileName;

            var metaData = metadataExtractor.GetMetadataFromFullPath(fullPath);

            metaData.Registry.Should().Be(registry);
            metaData.Ethnicity.Should().Be(ethnicity);
            metaData.Name.Should().Be(fileName);
        }

        [Test]
        public void GetMetadataFromFullPath_ContainsOneSubfolder_SetsOnlyRegistryAndName()
        {
            const string registry = "subfolder-1";
            const string fileName = "fileName";
            const string fullPath = registry + "/" + fileName;

            var metaData = metadataExtractor.GetMetadataFromFullPath(fullPath);

            metaData.Registry.Should().Be(registry);
            metaData.Name.Should().Be(fileName);
            metaData.Ethnicity.Should().BeNull();
        }

        [Test]
        public void GetMetadataFromFullPath_ContainsNoSubfolders_SetsOnlyName()
        {
            const string fileName = "fileName";

            var metaData = metadataExtractor.GetMetadataFromFullPath(fileName);

            metaData.Name.Should().Be(fileName);
            metaData.Registry.Should().BeNull();
            metaData.Ethnicity.Should().BeNull();
        }
    }
}