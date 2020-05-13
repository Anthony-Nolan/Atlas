using System.Net;
using System.Net.Http;
using Atlas.Utils.Core.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;
using Newtonsoft.Json.Linq;

namespace Atlas.Utils.Test.CoreUtils.Assertions
{
    public static class HttpResponseAssertionsExtensions
    {
        public static HttpResponseAssertions Should(this HttpResponseMessage subject)
        {
            return new HttpResponseAssertions(subject);
        }
    }

    public class HttpResponseAssertions : ReferenceTypeAssertions<HttpResponseMessage, HttpResponseAssertions>
    {
        public HttpResponseAssertions(HttpResponseMessage subject)
        {
            Subject = subject;
        }

        protected override string Context { get; } = nameof(HttpResponseMessage);

        public AndConstraint<HttpResponseAssertions> HaveStatusCode(HttpStatusCode expectedCode, string because = "",
            params object[] becauseArgs)
        {
            Execute.Assertion
                .ForCondition(Subject.StatusCode == expectedCode)
                .BecauseOf(because, becauseArgs)
                .FailWith("Expected status code {0} to be {1}{reason}.", Subject.StatusCode, expectedCode);
            return new AndConstraint<HttpResponseAssertions>(this);
        }

        public AndConstraint<HttpResponseAssertions> HaveAttachment(string filename, byte[] data, string contentType = null, string because = "",
            params object[] becauseArgs)
        {
            Subject.Content.Headers.ContentDisposition?.DispositionType.Should().Be("attachment");
            Subject.Content.Headers.ContentDisposition?.FileName.Should().Be(filename, because, becauseArgs);
            if (contentType != null)
            {
                Subject.Content.Headers.ContentType?.MediaType.Should().Be(contentType, because, becauseArgs);
            }
            var actualData = Subject.Content.ReadAsByteArrayAsync().RunSync();
            actualData.Should().Equal(data, because, becauseArgs);
            return new AndConstraint<HttpResponseAssertions>(this);
        }

        public JToken ContentAsJson()
        {
            var content = TaskUtils.RunSync(() => Subject.Content.ReadAsStringAsync());
            return JToken.Parse(content);
        }
    }
}
