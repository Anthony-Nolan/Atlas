using System.Linq;
using Atlas.DonorImport.Exceptions;
using Atlas.DonorImport.Services.DonorIdChecker;
using Atlas.DonorImport.Test.TestHelpers.Builders.DonorIdCheck;
using FluentAssertions;
using NUnit.Framework;

namespace Atlas.DonorImport.Test.Services.DonorChecker
{
    [TestFixture]
    internal class DonorIdCheckerFileParserTests
    {
        private IDonorIdCheckerFileParser donorIdCheckerFileParser;

        [SetUp]
        public void SetUp()
        {
            donorIdCheckerFileParser = new DonorIdCheckerFileParser();
        }

        [Test]
        public void ReadLazyDonorIds_ProcessesAllIds()
        {
            const int donorIdsCount = 100;
            var fileStream = DonorIdCheckFileContentsBuilder.New.WithDonorIds(donorIdsCount).Build().ToStream();

            var donorIds = donorIdCheckerFileParser.PrepareToLazilyParsingDonorIdFile(fileStream).ReadLazyDonorIds().ToList();

            donorIds.Should().HaveCount(donorIdsCount);
        }

        [Test]
        public void ReadLazyDonorIds_WhenInputStreamEmpty_ThrowsEmptyDonorFileException()
        {
            donorIdCheckerFileParser.Invoking(p => p.PrepareToLazilyParsingDonorIdFile(default).ReadLazyDonorIds().ToList())
                .Should().Throw<EmptyDonorFileException>();
        }
    }
}
