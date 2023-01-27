using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ServiceBus;
using Atlas.DonorImport.Data.Models;
using Atlas.DonorImport.Data.Repositories;
using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.DonorImport.Services.DonorUpdates;
using LochNessBuilder;
using Newtonsoft.Json;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.DonorImport.Test.Services.DonorUpdates
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
            await updatesRepository.Received(1).GetOldestUnpublishedDonorUpdates(2000);
        }

        [Test]
        public async Task PublishSearchableDonorUpdatesBatch_WhenNoUpdates_DoesNotPublish()
        {
            await updatesPublisher.PublishSearchableDonorUpdatesBatch();

            await messagePublisher.DidNotReceiveWithAnyArgs().BatchPublish(default);
        }

        [Test]
        public async Task PublishSearchableDonorUpdatesBatch_WhenNoUpdates_DoesNotMarkUpdatesAsPublished()
        {
            await updatesPublisher.PublishSearchableDonorUpdatesBatch();

            await updatesRepository.DidNotReceiveWithAnyArgs().MarkUpdatesAsPublished(default);
        }

        [Test]
        public async Task PublishSearchableDonorUpdatesBatch_PublishesUpdates()
        {
            const int updateCount = 2;

            updatesRepository.GetOldestUnpublishedDonorUpdates(default).ReturnsForAnyArgs(UpdateBuilder.Build(updateCount));

            await updatesPublisher.PublishSearchableDonorUpdatesBatch();

            await messagePublisher.Received(1).BatchPublish(Arg.Is<IEnumerable<SearchableDonorUpdate>>(x => x.Count() == updateCount));
        }

        [Test]
        public async Task PublishSearchableDonorUpdatesBatch_MarksUpdatesAsPublishedAfterPublishing()
        {
            var update = UpdateBuilder.Build(1).ToList();
            updatesRepository.GetOldestUnpublishedDonorUpdates(default).ReturnsForAnyArgs(update);

            await updatesPublisher.PublishSearchableDonorUpdatesBatch();

            Received.InOrder(() =>
                {
                    messagePublisher.Received(1).BatchPublish(Arg.Any<IEnumerable<SearchableDonorUpdate>>());
                    updatesRepository.Received(1).MarkUpdatesAsPublished(Arg.Is<IEnumerable<int>>(x => x.Single() == update.Single().Id));
                });
        }
    }
}
