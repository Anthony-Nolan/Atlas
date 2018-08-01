using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Owin.Testing;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.Validation
{
    [TestFixture]
    public class SmokeTests
    {
        [Test]
        public async Task ServiceStatusEndpoint_ReturnsOk()
        {
            using (var server = TestServer.Create<Startup>())
            {
                var result = await server.HttpClient.GetAsync("/service-status");
                result.StatusCode.Should().Be(HttpStatusCode.OK);
            }
        }
    }
}