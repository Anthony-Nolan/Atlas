using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Owin.Testing;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.Validation
{
    [TestFixture]
    public class Class1
    {
        [Test]
        public async Task Test()
        {
            using (var server = TestServer.Create<Startup>())
            {
                var result = await server.HttpClient.GetAsync("/service-status");
                result.StatusCode.Should().Be(HttpStatusCode.OK);
            }
        }
    }
}