﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Notifications;
using Atlas.DonorImport.ApplicationInsights;
using Atlas.DonorImport.Exceptions;
using Atlas.DonorImport.ExternalInterface;
using Atlas.DonorImport.FileSchema.Models.DonorChecker;
using Atlas.DonorImport.Services.DonorChecker;
using Atlas.DonorImport.Services.DonorIdChecker;
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
        private IDonorIdCheckerFileParser fileParser;
        private IDonorReader donorReader;
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
            donorReader = Substitute.For<IDonorReader>();
            blobStorageClient = Substitute.For<IDonorIdCheckerBlobStorageClient>();
            messageSender = Substitute.For<IDonorIdCheckerMessageSender>();
            notificationSender = Substitute.For<INotificationSender>();
            logger = Substitute.For<ILogger>();

            donorIdFile = Substitute.For<ILazilyParsingDonorIdFile>();
            donorIdFile.ReadLazyDonorIds().Returns(Enumerable.Empty<string>());
            fileParser.PrepareToLazilyParsingDonorIdFile(Arg.Any<Stream>())
                .Returns(donorIdFile);

            donorIdChecker = new DonorIdChecker(fileParser, donorReader, blobStorageClient, messageSender, notificationSender, logger);
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
            donorIdFile.ReadLazyDonorIds().Returns(Enumerable.Range(0, 100).Select(id => $"donor-id-{id}"));

            await donorIdChecker.CheckDonorIdsFromFile(DonorIdCheckFileBuilder.New.Build());

            await donorReader.Received().GetExistingExternalDonorCodes(Arg.Any<IEnumerable<string>>());
        }
        
        [Test]
        public async Task CheckDonorIdsFromFile_ReadsExternalDonorCodesBatches()
        {
            const int batchSize = 10000;
            const int numberOfCalls = 2;
            donorIdFile.ReadLazyDonorIds().Returns(Enumerable.Range(0, batchSize * numberOfCalls).Select(id => $"donor-id-{id}"));

            await donorIdChecker.CheckDonorIdsFromFile(DonorIdCheckFileBuilder.New.Build());

            await donorReader.Received(numberOfCalls).GetExistingExternalDonorCodes(Arg.Any<IEnumerable<string>>());
        }

        [Test]
        public async Task CheckDonorIdsFromFile_UploadsResults()
        {
            donorIdFile.ReadLazyDonorIds().Returns(Enumerable.Range(0, 100).Select(id => $"missed-id-{id}"));
            donorReader.GetExistingExternalDonorCodes(Arg.Any<IEnumerable<string>>()).Returns(new List<string>());

            await donorIdChecker.CheckDonorIdsFromFile(DonorIdCheckFileBuilder.New.Build());

            await blobStorageClient.Received().UploadResults(Arg.Any<DonorCheckerResults>(), Arg.Any<string>());
        }

        [Test]
        public async Task CheckDonorIdsFromFile_WhenNoResults_DoesNotUploadResults()
        {
            var donorRecordIds = Enumerable.Range(0, 100).Select(id => $"donor-id-{id}").ToList();
            donorIdFile.ReadLazyDonorIds().Returns(donorRecordIds);
            donorReader.GetExistingExternalDonorCodes(Arg.Any<IEnumerable<string>>()).Returns(donorRecordIds);

            await donorIdChecker.CheckDonorIdsFromFile(DonorIdCheckFileBuilder.New.Build());

            await blobStorageClient.DidNotReceive().UploadResults(Arg.Any<DonorCheckerResults>(), Arg.Any<string>());
        }

        [Test]
        public async Task CheckDonorIdsFromFile_SendsResultMessage()
        {
            var donorRecordIds = Enumerable.Range(0, 100).Select(id => $"donor-id-{id}").ToList();
            donorIdFile.ReadLazyDonorIds().Returns(donorRecordIds);
            donorReader.GetExistingExternalDonorCodes(Arg.Any<IEnumerable<string>>()).Returns(new List<string>());

            await donorIdChecker.CheckDonorIdsFromFile(DonorIdCheckFileBuilder.New.Build());

            await messageSender.Received().SendSuccessDonorCheckMessage(Arg.Any<string>(), Arg.Is<int>(v => v > 0), Arg.Any<string>());
        }

        [Test]
        public async Task CheckDonorIdsFromFile_WhenNoResults_SendsResultMessage()
        {
            var donorRecordIds = Enumerable.Range(0, 100).Select(id => $"donor-id-{id}").ToList();
            donorIdFile.ReadLazyDonorIds().Returns(donorRecordIds);
            donorReader.GetExistingExternalDonorCodes(Arg.Any<IEnumerable<string>>()).Returns(donorRecordIds);

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