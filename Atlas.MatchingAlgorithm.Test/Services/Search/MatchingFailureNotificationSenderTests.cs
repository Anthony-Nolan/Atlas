using Atlas.Client.Models.Search.Results.Matching;
using Atlas.MatchingAlgorithm.Clients.ServiceBus;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders;
using Atlas.MatchingAlgorithm.Services.Search;
using NSubstitute;
using NUnit.Framework;
using System.Threading.Tasks;

namespace Atlas.MatchingAlgorithm.Test.Services.Search
{
    [TestFixture]
    public class MatchingFailureNotificationSenderTests
    {
        private ISearchServiceBusClient searchServiceBusClient;

        private IMatchingFailureNotificationSender matchingFailureNotificationSender;

        [SetUp]
        public void SetUp()
        {
            searchServiceBusClient = Substitute.For<ISearchServiceBusClient>();
            var hlaNomenclatureVersionAccessor = Substitute.For<IActiveHlaNomenclatureVersionAccessor>();

            matchingFailureNotificationSender = new MatchingFailureNotificationSender(searchServiceBusClient, hlaNomenclatureVersionAccessor);
        }

        [Test]
        public async Task SendFailureNotification_PublishesFailureNotification()
        {
            const string searchRequestId = "search_id";
            const string validationError = "error message";

            await matchingFailureNotificationSender.SendFailureNotification(searchRequestId, 7, 3, validationError);

            await searchServiceBusClient.Received().PublishToResultsNotificationTopic(Arg.Any<MatchingResultsNotification>());

            await searchServiceBusClient.Received().PublishToResultsNotificationTopic(Arg.Is<MatchingResultsNotification>(r => 
                !r.WasSuccessful
                && r.SearchRequestId.Equals(searchRequestId)
                && r.ValidationError.Equals(validationError)));
        }
    }
}
