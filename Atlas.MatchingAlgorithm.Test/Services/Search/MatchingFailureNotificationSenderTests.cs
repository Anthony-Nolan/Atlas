using Atlas.Client.Models.Search.Results.Matching;
using Atlas.MatchingAlgorithm.Clients.ServiceBus;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders;
using Atlas.MatchingAlgorithm.Services.Search;
using NSubstitute;
using NUnit.Framework;
using System.Threading.Tasks;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.Test.TestHelpers.Builders.SearchRequests;

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
            const int attemptNumber = 7;
            const int remainingRetriesCount = 3;

            var searchRequest = new IdentifiedSearchRequest { Id = searchRequestId, SearchRequest = new SearchRequestBuilder().Build() };

            await matchingFailureNotificationSender.SendFailureNotification(searchRequest, attemptNumber, remainingRetriesCount, validationError);

            await searchServiceBusClient.Received().PublishToResultsNotificationTopic(Arg.Is<MatchingResultsNotification>(r =>
                !r.WasSuccessful
                && r.SearchRequestId.Equals(searchRequestId)
                && r.SearchRequest != null
                && r.ValidationError.Equals(validationError)
                && r.FailureInfo.AttemptNumber == attemptNumber
                && r.FailureInfo.RemainingRetriesCount == remainingRetriesCount
            ));
        }
    }
}
