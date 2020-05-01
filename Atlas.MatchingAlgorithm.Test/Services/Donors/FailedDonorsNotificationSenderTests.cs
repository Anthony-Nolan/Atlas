using Atlas.MatchingAlgorithm.ApplicationInsights;
using Atlas.MatchingAlgorithm.Models;
using Atlas.MatchingAlgorithm.Services.Donors;
using Atlas.MatchingAlgorithm.Test.TestHelpers.Builders;
using Atlas.Utils.Core.ApplicationInsights;
using Atlas.Utils.Notifications;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Atlas.MatchingAlgorithm.Test.Services.Donors
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
        public async Task SendFailedDonorsAlert_SendsAlertWithDonorCount()
        {
            const int totalDonorCount = 50;

            await failedDonorsNotificationSender.SendFailedDonorsAlert(
                FailedDonorInfoBuilder
                    .New()
                    .Build(totalDonorCount),
                "alert",
                Priority.Medium);

            await notificationsClient.Received().SendAlert(
                Arg.Is<Alert>(x => x.Description.Contains(totalDonorCount.ToString())));
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
