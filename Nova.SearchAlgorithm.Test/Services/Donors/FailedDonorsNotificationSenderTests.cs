using Nova.SearchAlgorithm.ApplicationInsights;
using Nova.SearchAlgorithm.Models;
using Nova.SearchAlgorithm.Services.Donors;
using Nova.SearchAlgorithm.Test.TestHelpers.Builders;
using Nova.Utils.ApplicationInsights;
using Nova.Utils.Notifications;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.Test.Services.Donors
{
    [TestFixture]
    public class FailedDonorsNotificationSenderTests
    {
        private IFailedDonorsNotificationSender failedDonorsNotificationSender;

        private INotificationsClient notificationsClient;
        private ILogger logger;

        [SetUp]
        public void SetUp()
        {
            notificationsClient = Substitute.For<INotificationsClient>();
            logger = Substitute.For<ILogger>();

            failedDonorsNotificationSender = new FailedDonorsNotificationSender(notificationsClient, logger);
        }

        [Test]
        public async Task SendFailedDonorsAlert_NoFailedDonors_DoesNotSendAlert()
        {
            await failedDonorsNotificationSender.SendFailedDonorsAlert(
                new List<FailedDonorInfo>(), "alert", Priority.Medium);

            await notificationsClient.DidNotReceive().SendAlert(Arg.Any<Alert>());
        }

        [Test]
        public async Task SendFailedDonorsAlert_SendsAlertWithAlertSummary()
        {
            const string alertSummary = "alert-summary";

            await failedDonorsNotificationSender.SendFailedDonorsAlert(
                FailedDonorInfoBuilder.New().Build(1),
                alertSummary,
                Priority.Medium);

            await notificationsClient.Received().SendAlert(
                Arg.Is<Alert>(x => x.Summary == alertSummary));
        }

        [Test]
        public async Task SendFailedDonorsAlert_SendsAlertWithLoggerPriority()
        {
            const Priority loggerPriority = Priority.High;

            await failedDonorsNotificationSender.SendFailedDonorsAlert(
                FailedDonorInfoBuilder.New().Build(1),
                "alert",
                loggerPriority);

            await notificationsClient.Received().SendAlert(
                Arg.Is<Alert>(x => x.Priority == loggerPriority));
        }


        [Test]
        public async Task SendFailedDonorsAlert_SendsAlertWithDonorCountByRegistryCode()
        {
            const string firstRegistry = "first";
            const string secondRegistry = "second";
            const int totalDonorCount = 50;
            const int perRegistryCount = totalDonorCount / 2;

            await failedDonorsNotificationSender.SendFailedDonorsAlert(
                FailedDonorInfoBuilder
                    .New(new[] { firstRegistry, secondRegistry })
                    .Build(totalDonorCount),
                "alert",
                Priority.Medium);

            await notificationsClient.Received().SendAlert(
                Arg.Is<Alert>(x =>
                    x.Description.Contains($"{firstRegistry} - {perRegistryCount}") &&
                    x.Description.Contains($"{secondRegistry} - {perRegistryCount}")));
        }

        [Test]
        public async Task SendFailedDonorsAlert_DonorsWithoutRegistryCode_SendsAlertWithUnknownRegistryCount()
        {
            const string unknownRegistryText = "[Unknown]";
            const string knownRegistry = "registry";
            const int totalDonorCount = 50;
            const int perRegistryCount = totalDonorCount / 2;

            await failedDonorsNotificationSender.SendFailedDonorsAlert(
                FailedDonorInfoBuilder
                    .New(new[] { string.Empty, knownRegistry })
                    .Build(totalDonorCount),
                "alert",
                Priority.Medium);

            await notificationsClient.Received().SendAlert(
                Arg.Is<Alert>(x =>
                    x.Description.Contains($"{unknownRegistryText} - {perRegistryCount}") &&
                    x.Description.Contains($"{knownRegistry} - {perRegistryCount}")));
        }

        [Test]
        public void SendFailedDonorsAlert_ExceptionThrownByNotificationsClient_DoesNotRethrowException()
        {
            notificationsClient.SendAlert(Arg.Any<Alert>()).Throws(new Exception());

            Assert.DoesNotThrowAsync(async() => await failedDonorsNotificationSender.SendFailedDonorsAlert(
                FailedDonorInfoBuilder.New().Build(1),
                "summary",
                Priority.Medium));
        }

        [Test]
        public async Task SendFailedDonorsAlert_ExceptionThrownByNotificationsClient_LogsNotificationSenderFailureEvent()
        {
            notificationsClient.SendAlert(Arg.Any<Alert>()).Throws(new Exception());

            await failedDonorsNotificationSender.SendFailedDonorsAlert(
                FailedDonorInfoBuilder.New().Build(1),
                "summary",
                Priority.Medium);

            logger.SendEvent(Arg.Any<NotificationSenderFailureEventModel>());
        }
    }
}
