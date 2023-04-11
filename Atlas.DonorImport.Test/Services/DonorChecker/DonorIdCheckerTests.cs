using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Notifications;
using Atlas.DonorImport.ApplicationInsights;
using Atlas.DonorImport.Data.Models;
using Atlas.DonorImport.Data.Repositories;
using Atlas.DonorImport.Exceptions;
using Atlas.DonorImport.FileSchema.Models;
using Atlas.DonorImport.FileSchema.Models.DonorChecker;
using Atlas.DonorImport.Services.DonorChecker;
using Atlas.DonorImport.Test.TestHelpers.Builders;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;

namespace Atlas.DonorImport.Test.Services.DonorChecker
{
    [TestFixture]
    internal class DonorIdCheckerTests
    {
        private const string RecordIdPrefix = "record-id-";

        private IDonorIdCheckerFileParser fileParser;
        private IDonorReadRepository donorReadRepository;
        private IDonorIdCheckerBlobStorageClient blobStorageClient;
        private IDonorIdCheckerMessageSender messageSender;
        private INotificationSender notificationSender;
        private ILogger logger;
        private ILazilyParsingDonorIdFile donorIdFile;

        private IDonorIdChecker donorIdChecker;

        [SetUp]
        public void SetUp()
        {
            fileParser = Substitute.For<IDonorIdCheckerFileParser>();
            donorReadRepository = Substitute.For<IDonorReadRepository>();
            blobStorageClient = Substitute.For<IDonorIdCheckerBlobStorageClient>();
            messageSender = Substitute.For<IDonorIdCheckerMessageSender>();
            notificationSender = Substitute.For<INotificationSender>();
            logger = Substitute.For<ILogger>();

            donorIdFile = Substitute.For<ILazilyParsingDonorIdFile>();
            donorIdFile.ReadLazyDonorIds().Returns(Enumerable.Empty<string>());
            fileParser.PrepareToLazilyParsingDonorIdFile(Arg.Any<Stream>())
                .Returns(donorIdFile);

            donorIdChecker = new DonorIdChecker(fileParser, donorReadRepository, blobStorageClient, messageSender, notificationSender, logger);
        }


        [Test]
        public async Task CheckDonorIdsFromFile_ReadsRegistryCodeAndDonorType()
        {
            var file = DonorIdCheckFileBuilder.New.Build();

            await donorIdChecker.CheckDonorIdsFromFile(file);

            donorIdFile.Received().ReadRegistryCodeAndDonorType();
        }

        [Test]
        public async Task CheckDonorIdsFromFile_ParsesInputFile()
        {
            var file = DonorIdCheckFileBuilder.New.Build();

            await donorIdChecker.CheckDonorIdsFromFile(file);
            
            fileParser.Received().PrepareToLazilyParsingDonorIdFile(file.Contents);
        }

        [Test]
        public async Task CheckDonorIdsFromFile_ReadsExternalDonorCodes()
        {
            const string registryCode = "registryCode";
            const ImportDonorType donorType = ImportDonorType.Adult;

            donorIdFile.ReadRegistryCodeAndDonorType().Returns((registryCode, donorType));

            await donorIdChecker.CheckDonorIdsFromFile(DonorIdCheckFileBuilder.New.Build());

            await donorReadRepository.Received().GetExternalDonorCodes(registryCode, DatabaseDonorType.Adult);
        }

        [Test]
        public async Task CheckDonorIdsFromFile_WhenAbsentDonors_UploadsResults()
        {
            var absentDonorIds = Enumerable.Range(0, 100).Select(id => $"absent-id-{id}").ToList();
            var presentDonorIds = Enumerable.Range(0, 100).Select(id => $"{RecordIdPrefix}{id}").ToList();
            donorIdFile.ReadLazyDonorIds().Returns(absentDonorIds.Concat(presentDonorIds));
            donorReadRepository.GetExternalDonorCodes(Arg.Any<string>(), Arg.Any<DatabaseDonorType>()).Returns(presentDonorIds);

            await donorIdChecker.CheckDonorIdsFromFile(DonorIdCheckFileBuilder.New.Build());

            await blobStorageClient.Received().UploadResults(Arg.Is<DonorIdCheckerResults>(r => r.AbsentRecordIds.Count == absentDonorIds.Count && absentDonorIds.All(id => r.AbsentRecordIds.Contains(id))), Arg.Any<string>());
        }

        [Test]
        public async Task CheckDonorIdsFromFile_WhenOrphanedDonors_UploadsResults()
        {
            var presentDonorIds = Enumerable.Range(0, 100).Select(id => $"{RecordIdPrefix}{id}").ToList();
            var orphanedDonorIds = Enumerable.Range(100, 100).Select(id => $"absent-id-{id}").ToList();
            donorIdFile.ReadLazyDonorIds().Returns(presentDonorIds);
            donorReadRepository.GetExternalDonorCodes(Arg.Any<string>(), Arg.Any<DatabaseDonorType>()).Returns(presentDonorIds.Concat(orphanedDonorIds).ToList());

            await donorIdChecker.CheckDonorIdsFromFile(DonorIdCheckFileBuilder.New.Build());

            await blobStorageClient.Received().UploadResults(Arg.Is<DonorIdCheckerResults>(r => r.OrphanedRecordIds.Count == orphanedDonorIds.Count && orphanedDonorIds.All(id => r.OrphanedRecordIds.Contains(id))), Arg.Any<string>());
        }

        [Test]
        public async Task CheckDonorIdsFromFile_WhenNoResults_DoesNotUploadResults()
        {
            var donorRecordIds = Enumerable.Range(0, 100).Select(id => $"{RecordIdPrefix}{id}").ToList();
            donorIdFile.ReadLazyDonorIds().Returns(donorRecordIds);
            donorReadRepository.GetExternalDonorCodes(Arg.Any<string>(), Arg.Any<DatabaseDonorType>()).Returns(donorRecordIds);

            await donorIdChecker.CheckDonorIdsFromFile(DonorIdCheckFileBuilder.New.Build());

            await blobStorageClient.DidNotReceive().UploadResults(Arg.Any<DonorCheckerResults>(), Arg.Any<string>());
        }

        [Test]
        public async Task CheckDonorIdsFromFile_SendsResultMessage()
        {
            var donorRecordIds = Enumerable.Range(0, 100).Select(id => $"{RecordIdPrefix}{id}").ToList();
            donorIdFile.ReadLazyDonorIds().Returns(donorRecordIds);
            donorReadRepository.GetExternalDonorCodes(Arg.Any<string>(), Arg.Any<DatabaseDonorType>()).Returns(new List<string>());

            await donorIdChecker.CheckDonorIdsFromFile(DonorIdCheckFileBuilder.New.Build());

            await messageSender.Received().SendSuccessDonorCheckMessage(Arg.Any<string>(), Arg.Is<int>(v => v > 0), Arg.Any<string>());
        }

        [Test]
        public async Task CheckDonorIdsFromFile_WhenNoResults_SendsResultMessage()
        {
            var donorRecordIds = Enumerable.Range(0, 100).Select(id => $"{RecordIdPrefix}{id}").ToList();
            donorIdFile.ReadLazyDonorIds().Returns(donorRecordIds);
            donorReadRepository.GetExternalDonorCodes(Arg.Any<string>(), Arg.Any<DatabaseDonorType>()).Returns(donorRecordIds);

            await donorIdChecker.CheckDonorIdsFromFile(DonorIdCheckFileBuilder.New.Build());
            
            await messageSender.Received().SendSuccessDonorCheckMessage(Arg.Any<string>(), 0, Arg.Any<string>());
        }

        [Test]
        public async Task CheckDonorIdsFromFile_WhenEmptyDonorFileException_SendsAlert()
        {
            donorIdFile.ReadLazyDonorIds().Throws<EmptyDonorFileException>();

            await donorIdChecker.CheckDonorIdsFromFile(DonorIdCheckFileBuilder.New.Build());

            await notificationSender.Received().SendAlert("Donor Ids file was present but it was empty.", Arg.Any<string>(), Priority.Medium, Arg.Any<string>());
        }

        [Test]
        public async Task CheckDonorIdsFromFile_WhenMalformedDonorFileException_SendsAlert()
        {
            var exception = new MalformedDonorFileException("Error message");
            donorIdFile.ReadLazyDonorIds().Throws(exception);

            await donorIdChecker.CheckDonorIdsFromFile(DonorIdCheckFileBuilder.New.Build());

            await notificationSender.Received().SendAlert(exception.Message, Arg.Any<string>(), Priority.Medium, Arg.Any<string>());
        }

        [Test]
        public void CheckDonorIdsFromFile_WhenUnexpectedException_LogsFailureEvent()
        {
            var exception = new Exception("Error message");
            donorIdFile.ReadLazyDonorIds().Throws(exception);
            
            donorIdChecker.Invoking(p => p.CheckDonorIdsFromFile(DonorIdCheckFileBuilder.New.Build()))
                .Should().Throw<Exception>();

            logger.Received().SendEvent(Arg.Any<DonorIdCheckFailureEventModel>());
        }
    }
}
