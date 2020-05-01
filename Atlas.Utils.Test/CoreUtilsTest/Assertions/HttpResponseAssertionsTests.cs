using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using Atlas.Utils.Test.CoreUtils.Assertions;
using NUnit.Framework;

namespace Atlas.Utils.Test.CoreUtilsTest.Assertions
{
    [TestFixture]
    public class HttpResponseAssertionsTests
    {
        [Test]
        public void GivenResponseExpectedStatusCode_ShouldHaveStatusCode_DoesNotThrow()
        {
            var response = new HttpResponseMessage { StatusCode = HttpStatusCode.Accepted };

            Action action = () => response.Should().HaveStatusCode(HttpStatusCode.Accepted);

            action.ShouldNotThrow();
        }

        [Test]
        public void GivenResponseUnexpectedStatusCode_ShouldHaveStatusCode_Throws()
        {
            var response = new HttpResponseMessage { StatusCode = HttpStatusCode.Accepted };

            Action action = () => response.Should().HaveStatusCode(HttpStatusCode.BadGateway);

            action.ShouldThrow<AssertionException>().Which.Message
                .Should().Be("Expected status code Accepted to be BadGateway.");
        }

        [Test]
        public void GivenResponseUnexpectedStatusCodeWithMessage_ShouldHaveStatusCode_Throws()
        {
            var response = new HttpResponseMessage { StatusCode = HttpStatusCode.Accepted };

            Action action = () => response.Should().HaveStatusCode(HttpStatusCode.BadGateway, "some {0} reason", "parameterised");

            action.ShouldThrow<AssertionException>().Which.Message
                .Should().Be("Expected status code Accepted to be BadGateway because some parameterised reason.");
        }

        [Test]
        public void GivenResponseWithExpectedAttachment_ShouldHaveAttachment_DoesNotThrow()
        {
            var content = new ByteArrayContent(new byte[] { 1, 2, 3 });
            content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
            {
                FileName = "test.dat"
            };
            content.Headers.ContentType = new MediaTypeHeaderValue("application/data");
            var response = new HttpResponseMessage { Content = content };

            Action action = () => response.Should().HaveAttachment("test.dat", new byte[] { 1, 2, 3 }, "application/data");

            action.ShouldNotThrow();
        }

        [Test]
        public void GivenAttachmentResponseWithWrongMediaType_ShouldHaveAttachment_Throws()
        {
            var content = new ByteArrayContent(new byte[] { 1, 2, 3 });
            content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
            {
                FileName = "test.dat"
            };
            content.Headers.ContentType = new MediaTypeHeaderValue("application/csv");
            var response = new HttpResponseMessage { Content = content };

            Action action = () => response.Should().HaveAttachment("test.dat", new byte[] { 1, 2, 3 }, "application/pdf");

            action.ShouldThrow<AssertionException>();
        }

        [Test]
        public void GivenAttachmentResponseWithWrongContent_ShouldHaveAttachment_Throws()
        {
            var content = new ByteArrayContent(new byte[] { 1, 2, 3 });
            content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
            {
                FileName = "test.dat"
            };
            content.Headers.ContentType = new MediaTypeHeaderValue("application/data");
            var response = new HttpResponseMessage { Content = content };

            Action action = () => response.Should().HaveAttachment("test.dat", new byte[] { 3, 2, 1 }, "application/data");

            action.ShouldThrow<AssertionException>().Which.Message
                .Should().Be("Expected collection to be equal to {0x03, 0x02, 0x01}, but {0x01, 0x02, 0x03} differs at index 0.");
        }

        [Test]
        public void GivenAttachmentResponseWithWrongFilename_ShouldHaveAttachment_Throws()
        {
            var content = new ByteArrayContent(new byte[] { 1, 2, 3 });
            content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
            {
                FileName = "test.dat"
            };
            content.Headers.ContentType = new MediaTypeHeaderValue("application/data");
            var response = new HttpResponseMessage { Content = content };

            Action action = () => response.Should().HaveAttachment("wrong.dat", new byte[] { 1, 2, 3 }, "application/data");

            action.ShouldThrow<AssertionException>();
        }

        [Test]
        public void GivenResponseWithJsonContent_ContentAsJson_ShouldReturnContentSynchronously()
        {
            var response = new HttpResponseMessage()
            {
                Content = new StringContent("{'key':'value'}")
            };

            var ret = response.Should().ContentAsJson();

            ret.ShouldBeEquivalentTo(JToken.Parse("{'key':'value'}"));
        }
    }
}
