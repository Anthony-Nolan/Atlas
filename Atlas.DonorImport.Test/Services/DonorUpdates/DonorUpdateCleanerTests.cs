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
            SetUpCleaner(null, 10, 1);

            await updatesCleaner.DeleteExpiredPublishedDonorUpdates();

            await updatesRepository.DidNotReceiveWithAnyArgs().DeleteUpdatesPublishedOnOrBefore(default, Arg.Is(10), Arg.Is(1));
        }

        [Test]
        public async Task DeleteExpiredPublishedDonorUpdates_DeletesUpdatesByExpectedCutOffDate()
        {
            const int expiryInDays = 50;
            const int batchSize = 100;
            const int batchCap = 1000;

            SetUpCleaner(expiryInDays, batchSize, batchCap);

            await updatesCleaner.DeleteExpiredPublishedDonorUpdates();

            await updatesRepository.Received().DeleteUpdatesPublishedOnOrBefore(Arg.Is<DateTimeOffset>(cutOffDate => 
                DateTimeOffset.Now.Subtract(cutOffDate).Days == expiryInDays ), Arg.Is(batchCap), Arg.Is(batchSize));
        }

        private void SetUpCleaner(int? expiryInDays, int batchSize, int batchCap)
        {
            var settings = new PublishDonorUpdatesSettings { PublishedUpdateExpiryInDays = expiryInDays, PublishedUpdatesToDeleteBatchSize = batchSize, PublishedUpdatesToDeleteCap = batchCap};
            updatesCleaner = new DonorUpdatesCleaner(updatesRepository, settings);
        }
    }
}