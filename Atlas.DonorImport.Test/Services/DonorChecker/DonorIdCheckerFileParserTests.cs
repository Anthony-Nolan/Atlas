using System.Linq;
using Atlas.DonorImport.Exceptions;
using Atlas.DonorImport.FileSchema.Models;
using Atlas.DonorImport.Services.DonorChecker;
using Atlas.DonorImport.Test.TestHelpers.Builders.DonorIdCheck;
using FluentAssertions;
using NSubstitute.ExceptionExtensions;
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

        [Test]
        [TestCase(ImportDonorType.Adult)]
        [TestCase(ImportDonorType.Cord)]
        public void ReadLazyDonorIds_ReadsRegistryCodeAndDonorType(ImportDonorType donorType)
        {
            const string donPool = "donPool";

            var fileStream = DonorIdCheckFileContentsBuilder.New
                .WithDonPool(donPool)
                .WithDonorType(donorType)
                .Build().ToStream();

            var (registryCode, parsedDonorType) = donorIdCheckerFileParser.PrepareToLazilyParsingDonorIdFile(fileStream).ReadRegistryCodeAndDonorType();

            registryCode.Should().Be(donPool);
            parsedDonorType.Should().Be(donorType);
        }


        [Test]
        public void ReadLazyDonorIds_WhenInvalidDonorType_ThrowsMalformedDonorFileException()
        {
            var fileStream = DonorIdCheckFileContentsBuilder.New
                .WithStringDonorType("invalid-type")
                .Build().ToStream();
            var lazyFile = donorIdCheckerFileParser.PrepareToLazilyParsingDonorIdFile(fileStream);

            lazyFile.Invoking(lf => lf.ReadRegistryCodeAndDonorType()).Should().Throw<MalformedDonorFileException>();
        }
    }
}
