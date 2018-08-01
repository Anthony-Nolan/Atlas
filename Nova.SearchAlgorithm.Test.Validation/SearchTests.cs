using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Owin.Testing;
using Newtonsoft.Json;
using Nova.SearchAlgorithm.Client.Models.SearchRequests;
using Nova.SearchAlgorithm.Client.Models.SearchResults;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Test.Integration.TestHelpers.Builders;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.Validation
{
    [TestFixture]
    public class SearchTests
    {
        private TestServer server;

        [OneTimeSetUp]
        public void SetUp()
        {
            server = TestServer.Create<Startup>();
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            server.Dispose();
        }

        [Test]
        public async Task Search_ReturnsDonors()
        {
            var searchRequest = new SearchRequestBuilder()
                .WithTotalMismatchCount(0)
                .WithLocusMismatchCount(Locus.A, 0)
                .WithLocusMismatchCount(Locus.B, 0)
                .WithLocusMismatchCount(Locus.Drb1, 0)
                .WithLocusMatchHla(Locus.A, TypePositions.One, "2")
                .WithLocusMatchHla(Locus.A, TypePositions.Two, "68")
                .WithLocusMatchHla(Locus.B, TypePositions.One, "7")
                .WithLocusMatchHla(Locus.B, TypePositions.Two, "44")
                .WithLocusMatchHla(Locus.Drb1, TypePositions.One, "01:MS")
                .WithLocusMatchHla(Locus.Drb1, TypePositions.Two, "12:MN")
                .WithLocusMatchHla(Locus.C, TypePositions.One, "04:01")
                .WithLocusMatchHla(Locus.C, TypePositions.Two, "15:02")
                .WithLocusMatchHla(Locus.Dqb1, TypePositions.One, "05:01")
                .WithLocusMatchHla(Locus.Dqb1, TypePositions.Two, "06:01")
                .Build();
            
            var result = await server.CreateRequest("/search")
                .AddHeader("X-Samples-ApiKey", "test-key")
                .And(request => request.Content = new StringContent(JsonConvert.SerializeObject(searchRequest), Encoding.UTF8, "application/json"))
                .PostAsync();
            var content = await result.Content.ReadAsStringAsync();
            var deserialisedContent = JsonConvert.DeserializeObject<SearchResultSet>(content);
            deserialisedContent.SearchResults.Count().Should().BeGreaterThan(0);
        }
        
        
    }
}