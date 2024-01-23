using System;
using System.Threading.Tasks;
using Atlas.Client.Models.SupportMessages;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Notifications;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;

namespace Atlas.Common.Test.Notifications
{
    [TestFixture]
    public class NotificationSenderTests
    {
        private INotificationSender sender;
        private INotificationsClient client;
        private ILogger logger;

        [SetUp]
        public void SetUp()
        {
            client = Substitute.For<INotificationsClient>();
            logger = Substitute.For<ILogger>();

            sender = new NotificationSender(client, logger);
        }

        [Test]
        public async Task SendFailedDonorsAlert_ExceptionThrownByNotificationsClient_DoesNotRethrowException()
        {
            client.SendAlert(default).ThrowsForAnyArgs(new Exception());

            await sender
                .Invoking(s => s.SendAlert("summary", "description", Priority.Medium, "source"))
                .Should().NotThrowAsync();
        }

        [Test]
        public async Task SendFailedDonorsAlert_ExceptionThrownByNotificationsClient_LogsNotificationSenderFailureEvent()
        {
            client.SendAlert(default).ThrowsForAnyArgs(new Exception());

            try
            {
                await sender.SendAlert("summary", "description", Priority.Medium, "source");
            }
            catch
            {
                // This shouldn't throw, but if it does, that's the responsibility of the other test
            }

            logger.Received().SendEvent(Arg.Any<NotificationSenderFailureEventModel>());
        }
    }
}
