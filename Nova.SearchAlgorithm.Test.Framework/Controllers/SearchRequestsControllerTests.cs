using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Client.Models.SearchRequests;
using Nova.SearchAlgorithm.Client.Models.SearchResults;
using Nova.SearchAlgorithm.Controllers;
using Nova.SearchAlgorithm.Services;
using Nova.Utils.TestUtils.Assertions;
using NSubstitute;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.Controllers
{
    [TestFixture]
    public class SearchRequestsControllerTests : ControllerTestBase<SearchRequestsController>
    {
        [SetUp]
        public void SetUp()
        {
            GetFake<ISearchService>().Search(Arg.Any<SearchRequest>())
                .Returns(Task.FromResult(Enumerable.Empty<SearchResult>()));
        }

        [Test]
        public async Task CreateSearchRequest_Returns200_WhenSearchRequestIsValidAdultSearch()
        {
            var response = await Server.CreateRequest("search")
                .And(req => req.Content = LoadContent("SearchRequests/validAdultSearchRequest.json")).PostAsync();

            response.Should().HaveStatusCode(HttpStatusCode.OK);
        }

        [Test]
        public async Task CreateSearchRequest_Returns200_WhenSearchRequestIsValidCordSearch()
        {
            var response = await Server.CreateRequest("search")
                .And(req => req.Content = LoadContent("SearchRequests/validCordSearchRequest.json")).PostAsync();

            response.Should().HaveStatusCode(HttpStatusCode.OK);
        }

        [Test]
        public async Task CreateSearchRequest_Returns400_WhenModelFailsValidation()
        {
            var response = await Server.CreateRequest("search")
                .And(req => req.Content = LoadContent("SearchRequests/invalidSearchRequest-empty.json")).PostAsync();

            response.Should().HaveStatusCode(HttpStatusCode.BadRequest);
        }

        [Test]
        public async Task CreateSearchRequest_Returns400_WhenRequestHasUnknownSearchType()
        {
            var response = await Server.CreateRequest("search")
                .And(req => req.Content = LoadContent("SearchRequests/invalidSearchRequest-badType.json")).PostAsync();

            response.Should().HaveStatusCode(HttpStatusCode.BadRequest);
        }

        [Test]
        public async Task CreateSearchRequest_Returns400_WhenRequestHasUnknownRegistry()
        {
            var response = await Server.CreateRequest("search")
                .And(req => req.Content = LoadContent("SearchRequests/invalidSearchRequest-badRegistry.json")).PostAsync();

            response.Should().HaveStatusCode(HttpStatusCode.BadRequest);
        }
    }
}
