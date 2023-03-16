using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Notifications;
using Atlas.DonorImport.ApplicationInsights;
using Atlas.DonorImport.Data.Models;
using Atlas.DonorImport.Data.Repositories;
using Atlas.DonorImport.FileSchema.Models;
using Atlas.DonorImport.FileSchema.Models.DonorChecker;
using Atlas.DonorImport.Services;
using Atlas.DonorImport.Services.DonorChecker;
using Atlas.DonorImport.Test.TestHelpers.Builders;
using Atlas.DonorImport.Test.TestHelpers.Builders.ExternalModels;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;

namespace Atlas.DonorImport.Test.Services.DonorChecker
{
    [TestFixture]
    public class DonorInfoCheckerTests
    {
        private IDonorImportFileParser fileParser;
        private IDonorReadRepository donorReadRepository;
        private IDonorInfoCheckerBlobStorageClient blobStorageClient;
        private IDonorInfoCheckerMessageSender messageSender;
        private INotificationSender notificationSender;
        private ILogger logger;
        private IDonorUpdateMapper donorUpdateMapper;

        private IDonorInfoChecker donorInfoChecker;

        [SetUp]
        public void SetUp()
        {
            fileParser = Substitute.For<IDonorImportFileParser>();
            donorReadRepository = Substitute.For<IDonorReadRepository>();
            blobStorageClient = Substitute.For<IDonorInfoCheckerBlobStorageClient>();
            messageSender = Substitute.For<IDonorInfoCheckerMessageSender>();
            notificationSender = Substitute.For<INotificationSender>();
            logger = Substitute.For<ILogger>();
            donorUpdateMapper = Substitute.For<IDonorUpdateMapper>();
            donorUpdateMapper.MapToDatabaseDonor(Arg.Any<DonorUpdate>(), Arg.Any<string>()).Returns(new Donor());

            donorInfoChecker = new DonorInfoChecker(fileParser, donorReadRepository, blobStorageClient, messageSender, notificationSender, logger, donorUpdateMapper);
        }

        [Test]
        public async Task CompareDonorInfoInFileToAtlasDonorStore_ShouldGetDonorsHashes()
        {
            var donors = DonorUpdateBuilder.New.Build(5);
            var file = DonorImportFileBuilder.NewWithoutContents.WithInitialDonors(donors.ToArray()).Build();
            fileParser.PrepareToLazilyParseDonorUpdates(file.Contents).Returns(new LazilyParsingDonorFile(file.Contents));

            await donorInfoChecker.CompareDonorInfoInFileToAtlasDonorStore(file);

            await donorReadRepository.Received().GetDonorsHashes(Arg.Any<IEnumerable<string>>());
        }

        [Test]
        public async Task CompareDonorInfoInFileToAtlasDonorStore_GetsDonorsHashesBatches()
        {
            const int batchSize = 10000;
            const int numberOfBatches = 2;
            var donors = DonorUpdateBuilder.New.Build(batchSize * numberOfBatches);
            var file = DonorImportFileBuilder.NewWithoutContents.WithInitialDonors(donors.ToArray()).Build();
            fileParser.PrepareToLazilyParseDonorUpdates(file.Contents).Returns(new LazilyParsingDonorFile(file.Contents));

            await donorInfoChecker.CompareDonorInfoInFileToAtlasDonorStore(file);

            await donorReadRepository.Received(numberOfBatches).GetDonorsHashes(Arg.Any<IEnumerable<string>>());
        }

        [Test]
        public async Task CompareDonorInfoInFileToAtlasDonorStore_CalculatesDonorHash()
        {
            const int numberOfDonors = 5;
            var donors = DonorUpdateBuilder.New.Build(numberOfDonors);
            var file = DonorImportFileBuilder.NewWithoutContents.WithInitialDonors(donors.ToArray()).Build();
            fileParser.PrepareToLazilyParseDonorUpdates(file.Contents).Returns(new LazilyParsingDonorFile(file.Contents));

            await donorInfoChecker.CompareDonorInfoInFileToAtlasDonorStore(file);

            donorUpdateMapper.Received(numberOfDonors).MapToDatabaseDonor(Arg.Any<DonorUpdate>(), Arg.Any<string>());
        }

        [Test]
        public async Task CompareDonorInfoInFileToAtlasDonorStore_UploadsResults()
        {
            var donor = DonorUpdateBuilder.New.Build();
            var file = DonorImportFileBuilder.NewWithoutContents.WithInitialDonors(donor).Build();
            fileParser.PrepareToLazilyParseDonorUpdates(file.Contents).Returns(new LazilyParsingDonorFile(file.Contents));
            donorReadRepository.GetDonorsHashes(Arg.Any<IEnumerable<string>>()).Returns(new Dictionary<string, string> { { donor.RecordId, "donorHash1" } });
            donorUpdateMapper.MapToDatabaseDonor(Arg.Any<DonorUpdate>(), Arg.Any<string>()).Returns(new Donor { ExternalDonorCode = donor.RecordId, Hash = "donorHash2" });

            await donorInfoChecker.CompareDonorInfoInFileToAtlasDonorStore(file);

            await blobStorageClient.Received().UploadResults(Arg.Any<DonorCheckerResults>(), Arg.Any<string>());
        }

        [Test]
        public async Task CompareDonorInfoInFileToAtlasDonorStore_WhenNoResults_DoesNotUploadResults()
        {
            const string donorHash = "donorHash";
            var donor = DonorUpdateBuilder.New.Build();
            var file = DonorImportFileBuilder.NewWithoutContents.WithInitialDonors(donor).Build();
            fileParser.PrepareToLazilyParseDonorUpdates(file.Contents).Returns(new LazilyParsingDonorFile(file.Contents));
            donorReadRepository.GetDonorsHashes(Arg.Any<IEnumerable<string>>()).Returns(new Dictionary<string, string> { { donor.RecordId, donorHash } });
            donorUpdateMapper.MapToDatabaseDonor(Arg.Any<DonorUpdate>(), Arg.Any<string>()).Returns(new Donor { ExternalDonorCode = donor.RecordId, Hash = donorHash });

            await donorInfoChecker.CompareDonorInfoInFileToAtlasDonorStore(file);

            await blobStorageClient.DidNotReceive().UploadResults(Arg.Any<DonorCheckerResults>(), Arg.Any<string>());
        }

        [Test]
        public async Task CompareDonorInfoInFileToAtlasDonorStore_SendsResultMessage()
        {
            var donor = DonorUpdateBuilder.New.Build();
            var file = DonorImportFileBuilder.NewWithoutContents.WithInitialDonors(donor).Build();
            fileParser.PrepareToLazilyParseDonorUpdates(file.Contents).Returns(new LazilyParsingDonorFile(file.Contents));

            await donorInfoChecker.CompareDonorInfoInFileToAtlasDonorStore(file);

            await messageSender.Received().SendSuccessDonorCheckMessage(file.FileLocation, Arg.Is<int>(v => v > 0), Arg.Any<string>());
        }

        [Test]
        public async Task CompareDonorInfoInFileToAtlasDonorStore_WhenNoResults_SendsResultMessage()
        {
            const string donorHash = "donorHash";
            var donor = DonorUpdateBuilder.New.Build();
            var file = DonorImportFileBuilder.NewWithoutContents.WithInitialDonors(donor).Build();
            fileParser.PrepareToLazilyParseDonorUpdates(file.Contents).Returns(new LazilyParsingDonorFile(file.Contents));
            donorReadRepository.GetDonorsHashes(Arg.Any<IEnumerable<string>>()).Returns(new Dictionary<string, string> { { donor.RecordId, donorHash } });
            donorUpdateMapper.MapToDatabaseDonor(Arg.Any<DonorUpdate>(), Arg.Any<string>()).Returns(new Donor { ExternalDonorCode = donor.RecordId, Hash = donorHash });

            await donorInfoChecker.CompareDonorInfoInFileToAtlasDonorStore(file);
            
            await messageSender.Received().SendSuccessDonorCheckMessage(file.FileLocation, 0, Arg.Any<string>());
        }

        [Test]
        public void CompareDonorInfoInFileToAtlasDonorStore_WhenUnexpectedException_LogsFailureEvent()
        {
            var donor = DonorUpdateBuilder.New.Build();
            var file = DonorImportFileBuilder.NewWithoutContents.WithInitialDonors(donor).Build();
            fileParser.PrepareToLazilyParseDonorUpdates(file.Contents).Returns(new LazilyParsingDonorFile(file.Contents));
            donorReadRepository.GetDonorsHashes(Arg.Any<IEnumerable<string>>()).Throws(new Exception("Error message"));

            donorInfoChecker.Invoking(c => c.CompareDonorInfoInFileToAtlasDonorStore(file)).Should().Throw<Exception>();

            logger.Received().SendEvent(Arg.Any<DonorComparerFailureEventModel>());
        }
    }
}
