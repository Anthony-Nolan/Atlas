using Atlas.Common.Notifications;
using Atlas.MatchingAlgorithm.Models;
using Atlas.MatchingAlgorithm.Services.Donors;
using Atlas.MatchingAlgorithm.Test.TestHelpers.Builders;
using NSubstitute;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Client.Models.SupportMessages;

namespace Atlas.MatchingAlgorithm.Test.Services.Donors
{
    [TestFixture]
    public class FailedDonorsNotificationSenderTests
    {
        private IFailedDonorsNotificationSender failedDonorsNotificationSender;

        private INotificationSender notificationSender;

        [SetUp]
        public void SetUp()
        {
            notificationSender = Substitute.For<INotificationSender>();

            failedDonorsNotificationSender = new FailedDonorsNotificationSender(notificationSender);
        }

        [Test]
        public async Task SendFailedDonorsAlert_NoFailedDonors_DoesNotSendAlert()
        {
            await failedDonorsNotificationSender.SendFailedDonorsAlert(
                new List<FailedDonorInfo>(), "alert", Priority.Medium);

            await notificationSender.DidNotReceiveWithAnyArgs().SendAlert(default, default, default, default);
        }

        [Test]
        public async Task SendFailedDonorsAlert_SendsAlertWithAlertSummary()
        {
            const string alertSummary = "alert-summary";

            await failedDonorsNotificationSender.SendFailedDonorsAlert(
                FailedDonorInfoBuilder.New().Build(1),
                alertSummary,
                Priority.Medium);

            await notificationSender.Received().SendAlert(
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

            await notificationSender.Received().SendAlert(
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

            await notificationSender.Received().SendAlert(
                Arg.Any<string>(),
                Arg.Is<string>(description => description.Contains(totalDonorCount.ToString())),
                Arg.Any<Priority>(),
                Arg.Any<string>()
            );
        }
    }
}
