using Nova.SearchAlgorithm.Models;
using Nova.SearchAlgorithm.Services.DonorManagement;
using Nova.Utils.Notifications;
using NSubstitute;
using NUnit.Framework;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.Test.Services.DonorManagement
{
    [TestFixture]
    public class DonorManagementNotificationSenderTests
    {
        private INotificationsClient notificationsClient;
        private IDonorManagementNotificationSender sender;

        [SetUp]
        public void Setup()
        {
            notificationsClient = Substitute.For<INotificationsClient>();

            sender = new DonorManagementNotificationSender(notificationsClient);
        }

        [Test]
        public async Task SendDonorUpdatesNotAppliedNotification_SendsNotificationWithUpdateDetails()
        {
            const int donorId = 12345;
            const long sequenceNumber = 45678;
            const bool availability = true;

            await sender.SendDonorUpdatesNotAppliedNotification(new DonorAvailabilityUpdate[]
            {
                new DonorAvailabilityUpdate
                {
                    DonorId = donorId,
                    UpdateSequenceNumber = sequenceNumber,
                    IsAvailableForSearch = availability
                }
            });

            await notificationsClient
                .Received(1)
                .SendNotification(Arg.Is<Notification>(x =>
                    x.Description.Contains(donorId.ToString()) &&
                    x.Description.Contains(sequenceNumber.ToString()) &&
                    x.Description.Contains(availability.ToString())));
        }
    }
}