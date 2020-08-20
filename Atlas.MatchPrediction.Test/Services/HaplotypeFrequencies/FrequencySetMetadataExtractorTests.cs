using System;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies.Import;
using FluentAssertions;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Services.HaplotypeFrequencies
{
    public class FrequencySetMetadataExtractorTests
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
            metadataExtractor.Invoking(service => service.GetFileNameFromPath(fullPath))
                .Should().Throw<Exception>();
        }

        [TestCase("0/1/2/fileName")]
        [TestCase("/1/2/fileName")]
        public void GetMetadataFromFullPath_ContainsMoreThanTwoSubfolders_ThrowsException(string fullPath)
        {
            metadataExtractor.Invoking(service => service.GetFileNameFromPath(fullPath))
                .Should().Throw<Exception>();
        }

        [Test]
        public void GetMetadataFromFullPath_ContainsTwoSubfolders_SetsRegistryEthnicityAndName()
        {
            const string registry = "subfolder-1";
            const string ethnicity = "subfolder-2";
            const string fileName = "fileName";
            const string fullPath = registry + "/" + ethnicity + "/" + fileName;

            var metaData = metadataExtractor.GetFileNameFromPath(fullPath);

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

            var metaData = metadataExtractor.GetFileNameFromPath(fullPath);

            metaData.Registry.Should().Be(registry);
            metaData.Name.Should().Be(fileName);
            metaData.Ethnicity.Should().BeNull();
        }

        [Test]
        public void GetMetadataFromFullPath_ContainsNoSubfolders_SetsOnlyName()
        {
            const string fileName = "fileName";

            var metaData = metadataExtractor.GetFileNameFromPath(fileName);

            metaData.Name.Should().Be(fileName);
            metaData.Registry.Should().BeNull();
            metaData.Ethnicity.Should().BeNull();
        }

        [TestCase("/blobServices/default/containers/haplotype-frequency-set-import/blobs/fileName", "fileName", null, null)]
        [TestCase("blobs/fileName", "fileName", null, null)]
        [TestCase("blobs/registry/fileName", "fileName", "registry", null)]
        [TestCase("blobs/registry/ethnicity/fileName", "fileName", "registry", "ethnicity")]
        [TestCase("blobs/blobs/fileName", "fileName", "blobs", null)]
        [TestCase("blobs/blobs/blobs/fileName", "fileName", "blobs", "blobs")]
        public void GetMetadataFromFullPath_IncludingBlobStorageInformation_ParsesMetadataCorrectly(
            string fullPath,
            string expectedFileName,
            string expectedRegistry,
            string expectedEthnicity
        )
        {
            var metadata = metadataExtractor.GetFileNameFromPath(fullPath);

            metadata.Name.Should().Be(expectedFileName);
            metadata.Registry.Should().Be(expectedRegistry);
            metadata.Ethnicity.Should().Be(expectedEthnicity);
        }
    }
}