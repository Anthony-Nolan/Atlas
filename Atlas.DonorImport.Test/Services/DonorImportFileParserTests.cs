using System.Linq;
using Atlas.DonorImport.Services;
using Atlas.DonorImport.Test.TestHelpers.Builders;
using FluentAssertions;
using NUnit.Framework;

namespace Atlas.DonorImport.Test.Services
{
    [TestFixture]
    internal class DonorImportFileParserTests
    {
        private IDonorImportFileParser donorImportFileParser;

        [SetUp]
        public void SetUp()
        {
            donorImportFileParser = new DonorImportFileParser();
        }

        [Test]
        public void ImportDonorFile_ProcessesAllDonors()
        {
            const int donorCount = 100;
            var fileStream = DonorImportFileContentsBuilder.New.WithDonorCount(donorCount).Build().ToStream();
            
            var donors = donorImportFileParser.PrepareToLazilyParseDonorUpdates(fileStream).ReadLazyDonorUpdates().ToList();

            donors.Count().Should().Be(donorCount);
        }
    }
}