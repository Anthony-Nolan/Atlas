using System.Linq;
using Atlas.DonorImport.Exceptions;
using Atlas.DonorImport.FileSchema.Models;
using Atlas.DonorImport.FileSchema.Models.DonorChecker;
using Atlas.DonorImport.Services.DonorChecker;
using Atlas.DonorImport.Test.TestHelpers.Builders.DonorIdCheck;
using Atlas.DonorImport.Test.TestHelpers.Models.DonorIdCheck;
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

        [Test]
        [TestCase(ImportDonorType.Adult)]
        [TestCase(ImportDonorType.Cord)]
        public void ReadLazyDonorIds_ReadsDonorType(ImportDonorType donorType)
        {
            var fileStream = DonorIdCheckFileContentsBuilder.New
                .WithDonorType(donorType)
                .Build().ToStream();
            var lazyFile = donorIdCheckerFileParser.PrepareToLazilyParsingDonorIdFile(fileStream);

            lazyFile.ReadLazyDonorIds().ToList();
            
            lazyFile.DonorType.Should().Be(donorType);
        }

        [Test]
        public void ReadLazyDonorIds_ReadsDonorPool()
        {
            const string newDonorPool = "newDonorPool";
            var fileStream = DonorIdCheckFileContentsBuilder.New
                .WithDonPool(newDonorPool)
                .Build().ToStream();
            var lazyFile = donorIdCheckerFileParser.PrepareToLazilyParsingDonorIdFile(fileStream);

            lazyFile.ReadLazyDonorIds().ToList();

            lazyFile.DonorPool.Should().Be(newDonorPool);
        }

        [Test]
        public void ReadLazyDonorIds_WhenInvalidDonorType_ThrowsMalformedDonorFileException()
        {
            var fileStream = DonorIdCheckFileContentsBuilder.New
                .WithStringDonorType("invalid-type")
                .Build()
                .ToStream();
            var lazyFile = donorIdCheckerFileParser.PrepareToLazilyParsingDonorIdFile(fileStream);

            lazyFile.Invoking(lf => lf.ReadLazyDonorIds().ToList())
                .Should()
                .Throw<MalformedDonorFileException>()
                .WithMessage($"Error parsing {nameof(DonorIdCheckerRequest.donorType)}.");
        }

        [Test]
        public void ReadLazyDonorIds_WhenInvalidPropertyOrder_ThrowsMalformedDonorFileException()
        {
            var fileStream = InvalidDonorIdCheckFileContentsBuilder.FileWithInvalidPropertyOrder
                .Build()
                .ToStream();
            var lazyFile = donorIdCheckerFileParser.PrepareToLazilyParsingDonorIdFile(fileStream);

            lazyFile.Invoking(lf => lf.ReadLazyDonorIds().ToList())
                .Should()
                .Throw<MalformedDonorFileException>();
        }
        
        [Test]
        public void ReadLazyDonorIds_WhenNoDonorPool_ThrowsMalformedDonorFileException()
        {
            var fileStream = DonorIdCheckFileContentsBuilder.New
                .WithDonPool(string.Empty)
                .Build()
                .ToStream();
            var lazyFile = donorIdCheckerFileParser.PrepareToLazilyParsingDonorIdFile(fileStream);

            lazyFile.Invoking(lf => lf.ReadLazyDonorIds().ToList())
                .Should()
                .Throw<MalformedDonorFileException>()
                .WithMessage($"{nameof(DonorIdCheckerRequest.donPool)} property must be defined before list of donors and cannot be null.");
        }

        [Test]
        public void ReadLazyDonorIds_WhenUnexpectedProperty_DoesNotThrowException()
        {
            var fileStream = InvalidDonorIdCheckFileContentsBuilder.FileWithUnexpectedProperty
                .Build()
                .ToStream();
            var lazyFile = donorIdCheckerFileParser.PrepareToLazilyParsingDonorIdFile(fileStream);

            lazyFile.Invoking(lf => lf.ReadLazyDonorIds().ToList())
                .Should()
                .NotThrow<MalformedDonorFileException>();
        }
    }
}
