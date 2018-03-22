using System.Net;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.Client.Models;
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
        [Test]
        public async Task CreateSearchRequest_Returns200_WhenSearchRequestIsSuccessfullyCreated()
        {
            GetFake<ISearchRequestService>().CreateSearchRequest(Arg.Any<SearchRequest>()).Returns(1);

            var response = await Server.CreateRequest("search")
                .And(req => req.Content = LoadContent("SearchRequests/searchrequestcreationmodel.json")).PostAsync();

            response.Should().HaveStatusCode(HttpStatusCode.OK);
        }

        [Test]
        public async Task CreateSearchRequest_Returns400_WhenModelFailsValidation()
        {
            GetFake<ISearchRequestService>().CreateSearchRequest(Arg.Any<SearchRequest>()).Returns(1);

            var response = await Server.CreateRequest("search")
                .And(req => req.Content = LoadContent("SearchRequests/invalidsearchrequestcreationmodel.json")).PostAsync();

            response.Should().HaveStatusCode(HttpStatusCode.BadRequest);
        }
    }
}
