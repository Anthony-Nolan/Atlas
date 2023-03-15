using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Notifications;
using Atlas.DonorImport.ApplicationInsights;
using Atlas.DonorImport.Data.Models;
using Atlas.DonorImport.ExternalInterface;
using Atlas.DonorImport.FileSchema.Models;
using Atlas.DonorImport.Services;
using Atlas.DonorImport.Services.DonorComparer;
using Atlas.DonorImport.Test.TestHelpers.Builders;
using Atlas.DonorImport.Test.TestHelpers.Builders.ExternalModels;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;

namespace Atlas.DonorImport.Test.Services.DonorComparer
{
    [TestFixture]
    public class DonorComparerTests
    {
        private IDonorImportFileParser fileParser;
        private IDonorReader donorReader;
        private IDonorCheckerBlobStorageClient blobStorageClient;
        private IDonorCheckerMessageSender messageSender;
        private INotificationSender notificationSender;
        private ILogger logger;
        private IDonorRecordChangeApplier donorRecordChangeApplier;

        private IDonorComparer donorComparer;

        [SetUp]
        public void SetUp()
        {
            fileParser = Substitute.For<IDonorImportFileParser>();
            donorReader = Substitute.For<IDonorReader>();
            blobStorageClient = Substitute.For<IDonorCheckerBlobStorageClient>();
            messageSender = Substitute.For<IDonorCheckerMessageSender>();
            notificationSender = Substitute.For<INotificationSender>();
            logger = Substitute.For<ILogger>();
            donorRecordChangeApplier = Substitute.For<IDonorRecordChangeApplier>();
            donorRecordChangeApplier.MapToDatabaseDonor(Arg.Any<DonorUpdate>(), Arg.Any<string>()).Returns(new Donor());

            donorComparer = new DonorImport.Services.DonorComparer.DonorComparer(fileParser, donorReader, blobStorageClient, messageSender, notificationSender, logger, donorRecordChangeApplier);
        }

        [Test]
        public async Task CompareDonorsFromFile_ShouldGetDonorsHashes()
        {
            var donors = DonorUpdateBuilder.New.Build(5);
            var file = DonorImportFileBuilder.NewWithoutContents.WithInitialDonors(donors.ToArray()).Build();
            fileParser.PrepareToLazilyParseDonorUpdates(file.Contents).Returns(new LazilyParsingDonorFile(file.Contents));

            await donorComparer.CompareDonorInfoInFileToAtlasDonorStore(file);

            await donorReader.Received().GetDonorsHashes(Arg.Any<IEnumerable<string>>());
        }

        [Test]
        public async Task CompareDonorsFromFile_ShouldGetDonorsHashesBatches()
        {
            const int batchSize = 10000;
            const int numberOfButches = 2;
            var donors = DonorUpdateBuilder.New.Build(batchSize * numberOfButches);
            var file = DonorImportFileBuilder.NewWithoutContents.WithInitialDonors(donors.ToArray()).Build();
            fileParser.PrepareToLazilyParseDonorUpdates(file.Contents).Returns(new LazilyParsingDonorFile(file.Contents));

            await donorComparer.CompareDonorInfoInFileToAtlasDonorStore(file);

            await donorReader.Received(numberOfButches).GetDonorsHashes(Arg.Any<IEnumerable<string>>());
        }

        [Test]
        public async Task CompareDonorsFromFile_CalculatesDonorHash()
        {
            const int numberOfDonors = 5;
            var donors = DonorUpdateBuilder.New.Build(numberOfDonors);
            var file = DonorImportFileBuilder.NewWithoutContents.WithInitialDonors(donors.ToArray()).Build();
            fileParser.PrepareToLazilyParseDonorUpdates(file.Contents).Returns(new LazilyParsingDonorFile(file.Contents));

            await donorComparer.CompareDonorInfoInFileToAtlasDonorStore(file);

            donorRecordChangeApplier.Received(numberOfDonors).MapToDatabaseDonor(Arg.Any<DonorUpdate>(), Arg.Any<string>());
        }

        [Test]
        public async Task CompareDonorsFromFile_UploadsResults()
        {
            var donor = DonorUpdateBuilder.New.Build();
            var file = DonorImportFileBuilder.NewWithoutContents.WithInitialDonors(donor).Build();
            fileParser.PrepareToLazilyParseDonorUpdates(file.Contents).Returns(new LazilyParsingDonorFile(file.Contents));
            donorReader.GetDonorsHashes(Arg.Any<IEnumerable<string>>()).Returns(new Dictionary<string, string> { { donor.RecordId, "donorHash1" } });
            donorRecordChangeApplier.MapToDatabaseDonor(Arg.Any<DonorUpdate>(), Arg.Any<string>()).Returns(new Donor { Hash = "donorHash2" });

            await donorComparer.CompareDonorInfoInFileToAtlasDonorStore(file);

            await blobStorageClient.Received().UploadDonorInfoCheckerResults(Arg.Any<DonorComparerResults>(), Arg.Any<string>());
        }

        [Test]
        public async Task CompareDonorsFromFile_WhenNoMismatches_NotUploadsResults()
        {
            const string donorHash = "donorHash";
            var donor = DonorUpdateBuilder.New.Build();
            var file = DonorImportFileBuilder.NewWithoutContents.WithInitialDonors(donor).Build();
            fileParser.PrepareToLazilyParseDonorUpdates(file.Contents).Returns(new LazilyParsingDonorFile(file.Contents));
            donorReader.GetDonorsHashes(Arg.Any<IEnumerable<string>>()).Returns(new Dictionary<string, string> { { donor.RecordId, donorHash } });
            donorRecordChangeApplier.MapToDatabaseDonor(Arg.Any<DonorUpdate>(), Arg.Any<string>()).Returns(new Donor { Hash = donorHash });
            
            await donorComparer.CompareDonorInfoInFileToAtlasDonorStore(file);

            await blobStorageClient.DidNotReceive().UploadDonorInfoCheckerResults(Arg.Any<DonorComparerResults>(), Arg.Any<string>());
        }

        [Test]
        public async Task CompareDonorsFromFile_SendsResultMessage()
        {
            var donor = DonorUpdateBuilder.New.Build();
            var file = DonorImportFileBuilder.NewWithoutContents.WithInitialDonors(donor).Build();
            fileParser.PrepareToLazilyParseDonorUpdates(file.Contents).Returns(new LazilyParsingDonorFile(file.Contents));

            await donorComparer.CompareDonorInfoInFileToAtlasDonorStore(file);

            await messageSender.Received().SendSuccessDonorInfoCheckMessage(file.FileLocation, Arg.Any<int>(), Arg.Any<string>());
        }


        [Test]
        public void CompareDonorsFromFile_WhenUnexpectedException_LogsFailureEvent()
        {
            var donor = DonorUpdateBuilder.New.Build();
            var file = DonorImportFileBuilder.NewWithoutContents.WithInitialDonors(donor).Build();
            fileParser.PrepareToLazilyParseDonorUpdates(file.Contents).Returns(new LazilyParsingDonorFile(file.Contents));
            donorReader.GetDonorsHashes(Arg.Any<IEnumerable<string>>()).Throws(new Exception("Error message"));
            
            donorComparer.Invoking(c => c.CompareDonorInfoInFileToAtlasDonorStore(file)).Should().Throw<Exception>();

            logger.Received().SendEvent(Arg.Any<DonorComparerFailureEventModel>());
        }
    }
}
