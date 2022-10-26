using System;
using System.Threading.Tasks;
using Atlas.DonorImport.Data.Repositories;
using Atlas.DonorImport.ExternalInterface.Settings;
using Atlas.DonorImport.Services.DonorUpdates;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.DonorImport.Test.Services.DonorUpdates
{
    [TestFixture]
    internal class DonorUpdateCleanerTests
    {
        private IPublishableDonorUpdatesRepository updatesRepository;
        private IDonorUpdatesCleaner updatesCleaner;

        [SetUp]
        public void SetUp()
        {
            updatesRepository = Substitute.For<IPublishableDonorUpdatesRepository>();
            // set up cleaner service within each test to control what settings are passed in
        }

        [Test]
        public async Task DeleteExpiredPublishedDonorUpdates_NoExpirySet_DoesNotDeleteUpdates()
        {
            SetUpCleaner(null);

            await updatesCleaner.DeleteExpiredPublishedDonorUpdates();

            await updatesRepository.DidNotReceiveWithAnyArgs().DeleteUpdatesPublishedOnOrBefore(default);
        }

        [Test]
        public async Task DeleteExpiredPublishedDonorUpdates_DeletesUpdatesByExpectedCutOffDate()
        {
            const int expiryInDays = 50;

            SetUpCleaner(expiryInDays);

            await updatesCleaner.DeleteExpiredPublishedDonorUpdates();

            await updatesRepository.Received().DeleteUpdatesPublishedOnOrBefore(Arg.Is<DateTimeOffset>(cutOffDate => 
                DateTimeOffset.Now.Subtract(cutOffDate).Days == expiryInDays ));
        }

        private void SetUpCleaner(int? expiryInDays)
        {
            var settings = new PublishDonorUpdatesSettings { PublishedUpdateExpiryInDays = expiryInDays };
            updatesCleaner = new DonorUpdatesCleaner(updatesRepository, settings);
        }
    }
}