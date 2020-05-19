using System.Threading.Tasks;
using Atlas.MatchingAlgorithm.Client.Models.SearchRequests;
using Atlas.MatchingAlgorithm.Clients.ServiceBus;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.Models;
using Atlas.MatchingAlgorithm.Services.Search;
using Atlas.MatchingAlgorithm.Test.Builders;
using FluentValidation;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Services.Search
{
    [TestFixture]
    public class SearchDispatcherTests
    {
        private ISearchServiceBusClient searchServiceBusClient;

        private SearchDispatcher searchDispatcher;

        [SetUp]
        public void SetUp()
        {
            searchServiceBusClient = Substitute.For<ISearchServiceBusClient>();

            searchDispatcher = new SearchDispatcher(searchServiceBusClient);
        }

        [Test]
        public async Task DispatchSearch_DispatchesSearchWithId()
        {
            await searchDispatcher.DispatchSearch(
                new SearchRequestBuilder()
                    .WithSearchHla(new PhenotypeInfo<string>("hla-type"))
                    .WithTotalMismatchCount(0)
                    .Build());

            await searchServiceBusClient.Received().PublishToSearchQueue(Arg.Is<IdentifiedSearchRequest>(r => r.Id != null));
        }

        [Test]
        public void DispatchSearch_ValidatesSearchRequest()
        {
            var invalidSearchRequest = new SearchRequest();

            Assert.ThrowsAsync<ValidationException>(() => searchDispatcher.DispatchSearch(invalidSearchRequest));
        }
    }
}