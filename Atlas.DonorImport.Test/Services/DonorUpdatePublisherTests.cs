using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ServiceBus;
using Atlas.DonorImport.Data.Models;
using Atlas.DonorImport.Data.Repositories;
using Atlas.DonorImport.Services;
using Atlas.MatchingAlgorithm.Client.Models.Donors;
using LochNessBuilder;
using Newtonsoft.Json;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.DonorImport.Test.Services
{
    [TestFixture]
    internal class DonorUpdatePublisherTests
    {
        private IMessageBatchPublisher<SearchableDonorUpdate> messagePublisher;
        private IPublishableDonorUpdatesRepository updatesRepository;
        private IDonorUpdatesPublisher updatesPublisher;

        private static readonly Builder<PublishableDonorUpdate> UpdateBuilder = Builder<PublishableDonorUpdate>.New
            .With(x => x.Id, 123)
            .With(x => x.SearchableDonorUpdate, JsonConvert.SerializeObject(new SearchableDonorUpdate()));

        [SetUp]
        public void SetUp()
        {
            messagePublisher = Substitute.For<IMessageBatchPublisher<SearchableDonorUpdate>>();
            updatesRepository = Substitute.For<IPublishableDonorUpdatesRepository>();
            updatesPublisher = new DonorUpdatesPublisher(messagePublisher, updatesRepository);
        }

        [Test]
        public async Task PublishSearchableDonorUpdatesBatch_GetBatchOfOldestUpdates()
        {
            await updatesPublisher.PublishSearchableDonorUpdatesBatch();

            // 2000 is current hard-coded batch size
            await updatesRepository.Received(1).GetOldestDonorUpdates(2000);
        }

        [Test]
        public async Task PublishSearchableDonorUpdatesBatch_WhenNoUpdates_DoesNotPublish()
        {
            await updatesPublisher.PublishSearchableDonorUpdatesBatch();

            await messagePublisher.DidNotReceiveWithAnyArgs().BatchPublish(default);
        }

        [Test]
        public async Task PublishSearchableDonorUpdatesBatch_WhenNoUpdates_DoesNotDeleteUpdates()
        {
            await updatesPublisher.PublishSearchableDonorUpdatesBatch();

            await updatesRepository.DidNotReceiveWithAnyArgs().BulkInsert(default);
        }

        [Test]
        public async Task PublishSearchableDonorUpdatesBatch_PublishesUpdates()
        {
            const int updateCount = 2;

            updatesRepository.GetOldestDonorUpdates(default).ReturnsForAnyArgs(UpdateBuilder.Build(updateCount));

            await updatesPublisher.PublishSearchableDonorUpdatesBatch();

            await messagePublisher.Received(1).BatchPublish(Arg.Is<IEnumerable<SearchableDonorUpdate>>(x => x.Count() == updateCount));
        }

        [Test]
        public async Task PublishSearchableDonorUpdatesBatch_DeleteUpdatesAfterPublishing()
        {
            var update = UpdateBuilder.Build(1).ToList();
            updatesRepository.GetOldestDonorUpdates(default).ReturnsForAnyArgs(update);

            await updatesPublisher.PublishSearchableDonorUpdatesBatch();

            Received.InOrder( () =>
                {
                    messagePublisher.Received(1).BatchPublish(Arg.Any<IEnumerable<SearchableDonorUpdate>>());
                    updatesRepository.Received(1).DeleteDonorUpdates(Arg.Is<IEnumerable<int>>(x => x.Single() == update.Single().Id));
                });
        }
    }
}
