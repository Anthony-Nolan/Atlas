using Atlas.Common.ApplicationInsights;
using Atlas.Common.Notifications;
using Atlas.MatchingAlgorithm.Models;
using Atlas.MatchingAlgorithm.Services.Donors;
using Atlas.MatchingAlgorithm.Test.TestHelpers.Builders;
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

        private INotificationSender notificationsClient;
        private ILogger logger;

        [SetUp]
        public void SetUp()
        {
            notificationsClient = Substitute.For<INotificationSender>();
            logger = Substitute.For<ILogger>();

            failedDonorsNotificationSender = new FailedDonorsNotificationSender(notificationsClient);
        }

        [Test]
        public async Task SendFailedDonorsAlert_NoFailedDonors_DoesNotSendAlert()
        {
            await failedDonorsNotificationSender.SendFailedDonorsAlert(
                new List<FailedDonorInfo>(), "alert", Priority.Medium);

            await notificationsClient.DidNotReceiveWithAnyArgs().SendAlert(default, default, default, default);
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
                Arg.Is<string>(summary => summary == alertSummary),
                Arg.Any<string>(),
                Arg.Any<Priority>(),
                Arg.Any<string>()
            );
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
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Is<Priority>(pri => pri == loggerPriority),
                Arg.Any<string>()
            );
        }


        [Test]
        public async Task SendFailedDonorsAlert_SendsAlertWithDonorCount()
        {
            const int totalDonorCount = 50;

            await failedDonorsNotificationSender.SendFailedDonorsAlert(
                FailedDonorInfoBuilder.New().Build(totalDonorCount),
                "alert",
                Priority.Medium);

            await notificationsClient.Received().SendAlert(
                Arg.Any<string>(),
                Arg.Is<string>(description => description.Contains(totalDonorCount.ToString())),
                Arg.Any<Priority>(),
                Arg.Any<string>()
            );
        }
    }
}
