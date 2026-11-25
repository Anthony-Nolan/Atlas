using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Atlas.HlaMetadataDictionary.ExternalInterface.Settings;
using Atlas.HlaMetadataDictionary.WmdaDataAccess;
using AutoFixture;
using AwesomeAssertions;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.HlaMetadataDictionary.Test.UnitTests.WmdaDataAccess;

[TestFixture]
public class WmdaFileDownloaderTests
{
    private Fixture fixture;
    private FakeHttpMessageHandler handler;
    private string sourceUrl;
    private string version;
    private string fileName;
    private TimeSpan clientTimeout;
    private WmdaFileDownloader downloader;

    [SetUp]
    public void SetUp()
    {
        fixture = new Fixture();
        handler = new FakeHttpMessageHandler();
        sourceUrl = fixture.Create<Uri>().ToString();
        version = fixture.Create<string>();
        fileName = fixture.Create<string>();
        clientTimeout = WmdaFileDownloader.DownloadTimeout;

        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        httpClientFactory.CreateClient(WmdaFileDownloader.HttpClientName)
            .Returns(_ => new HttpClient(handler, disposeHandler: false) { Timeout = clientTimeout });

        downloader = new WmdaFileDownloader(httpClientFactory, new HlaMetadataDictionarySettings { HlaNomenclatureSourceUrl = sourceUrl });
    }

    [TearDown]
    public void TearDown()
    {
        handler.Dispose();
    }

    private string FileAddress => $"{sourceUrl}{version}/{fileName}";

    [Test]
    public void GetFileContentsWithoutHeader_RequestsFileOfVersionFromSourceUrl()
    {
        downloader.GetFileContentsWithoutHeader(version, fileName).ToList();

        handler.RequestUris.Should().ContainSingle().Which.Should().Be(new Uri(FileAddress));
    }

    [Test]
    public void GetFileContentsWithoutHeader_ReturnsLinesAfterLeadingCommentLines()
    {
        var lines = fixture.CreateMany<string>(3).ToList();
        handler.Body = $"#{fixture.Create<string>()}\n#{fixture.Create<string>()}\n{string.Join('\n', lines)}";

        var contents = downloader.GetFileContentsWithoutHeader(version, fileName);

        contents.Should().Equal(lines);
    }

    [Test]
    public void GetFileContentsWithoutHeader_KeepsCommentLinesAfterFirstDataLine()
    {
        var dataLine = fixture.Create<string>();
        var laterCommentLine = $"#{fixture.Create<string>()}";
        handler.Body = $"#{fixture.Create<string>()}\n{dataLine}\n{laterCommentLine}";

        var contents = downloader.GetFileContentsWithoutHeader(version, fileName);

        contents.Should().Equal(dataLine, laterCommentLine);
    }

    [Test]
    public void GetFileContentsWithoutHeader_SplitsOnLineFeedOnly()
    {
        var firstLine = fixture.Create<string>();
        var secondLine = fixture.Create<string>();
        handler.Body = $"{firstLine}\r\n{secondLine}\n";

        var contents = downloader.GetFileContentsWithoutHeader(version, fileName);

        contents.Should().Equal($"{firstLine}\r", secondLine, "");
    }

    [Test]
    public void GetFileContentsWithoutHeader_DisposesResponse()
    {
        downloader.GetFileContentsWithoutHeader(version, fileName).ToList();

        handler.LastResponse.IsDisposed.Should().BeTrue();
    }

    [TestCase(HttpStatusCode.NotFound)]
    [TestCase(HttpStatusCode.InternalServerError)]
    public void GetFileContentsWithoutHeader_WhenStatusIsNotSuccess_ThrowsWithFileAddress(HttpStatusCode statusCode)
    {
        handler.StatusCode = statusCode;
        handler.Body = fixture.Create<string>();

        var act = () => downloader.GetFileContentsWithoutHeader(version, fileName).ToList();

        act.Should().ThrowExactly<HttpRequestException>()
            .WithMessage($"*{FileAddress}*")
            .Which.StatusCode.Should().Be(statusCode);
    }

    [Test]
    public void GetFileContentsWithoutHeader_WhenStatusIsNotSuccess_DisposesResponse()
    {
        handler.StatusCode = HttpStatusCode.NotFound;

        var act = () => downloader.GetFileContentsWithoutHeader(version, fileName).ToList();

        act.Should().Throw<HttpRequestException>();
        handler.LastResponse.IsDisposed.Should().BeTrue();
    }

    [Test]
    public void GetFileContentsWithoutHeader_WhenSendFails_ThrowsWithFileAddress()
    {
        var sendException = new HttpRequestException(fixture.Create<string>());
        handler.SendException = sendException;

        var act = () => downloader.GetFileContentsWithoutHeader(version, fileName).ToList();

        act.Should().ThrowExactly<HttpRequestException>()
            .WithMessage($"*{FileAddress}*")
            .Which.InnerException.Should().BeSameAs(sendException);
    }

    [Test]
    public void GetFileContentsWithoutHeader_WhenClientTimesOut_ThrowsWithFileAddress()
    {
        clientTimeout = TimeSpan.FromMilliseconds(50);
        handler.WaitsUntilCancelled = true;

        var act = () => downloader.GetFileContentsWithoutHeader(version, fileName).ToList();

        act.Should().ThrowExactly<HttpRequestException>()
            .WithMessage($"*{FileAddress}*")
            .WithInnerExceptionExactly<TaskCanceledException>();
    }

    [Test]
    public void GetFirstNonCommentLine_ReturnsFirstLineAfterCommentLines()
    {
        var firstLine = fixture.Create<string>();
        handler.Body = $"#{fixture.Create<string>()}\n#{fixture.Create<string>()}\n{firstLine}\n{fixture.Create<string>()}";

        var line = downloader.GetFirstNonCommentLine(version, fileName);

        line.Should().Be(firstLine);
    }

    [Test]
    public void GetFirstNonCommentLine_ReadsWholeContentInsideClientTimeout()
    {
        var laterLines = fixture.CreateMany<string>(1000);
        handler.Body = $"{fixture.Create<string>()}\n{string.Join('\n', laterLines)}";

        downloader.GetFirstNonCommentLine(version, fileName);

        handler.LastContentStream.WasReadToEnd.Should().BeTrue();
    }

    [Test]
    public void GetFirstNonCommentLine_WhenFileHasOnlyCommentLines_ThrowsWithFileAddress()
    {
        handler.Body = $"#{fixture.Create<string>()}\n#{fixture.Create<string>()}";

        var act = () => downloader.GetFirstNonCommentLine(version, fileName);

        act.Should().ThrowExactly<Exception>().WithMessage($"*{FileAddress}*");
    }

    [Test]
    public void GetFirstNonCommentLine_DisposesResponse()
    {
        handler.Body = fixture.Create<string>();

        downloader.GetFirstNonCommentLine(version, fileName);

        handler.LastResponse.IsDisposed.Should().BeTrue();
    }

    [TestCase(HttpStatusCode.NotFound)]
    [TestCase(HttpStatusCode.InternalServerError)]
    public void GetFirstNonCommentLine_WhenStatusIsNotSuccess_ThrowsWithFileAddress(HttpStatusCode statusCode)
    {
        handler.StatusCode = statusCode;
        handler.Body = fixture.Create<string>();

        var act = () => downloader.GetFirstNonCommentLine(version, fileName);

        act.Should().ThrowExactly<HttpRequestException>()
            .WithMessage($"*{FileAddress}*")
            .Which.StatusCode.Should().Be(statusCode);
    }

    [Test]
    public void GetFirstNonCommentLine_WhenSendFails_ThrowsWithFileAddress()
    {
        var sendException = new HttpRequestException(fixture.Create<string>());
        handler.SendException = sendException;

        var act = () => downloader.GetFirstNonCommentLine(version, fileName);

        act.Should().ThrowExactly<HttpRequestException>()
            .WithMessage($"*{FileAddress}*")
            .Which.InnerException.Should().BeSameAs(sendException);
    }

    [Test]
    public void GetFirstNonCommentLine_WhenClientTimesOut_ThrowsWithFileAddress()
    {
        clientTimeout = TimeSpan.FromMilliseconds(50);
        handler.WaitsUntilCancelled = true;

        var act = () => downloader.GetFirstNonCommentLine(version, fileName);

        act.Should().ThrowExactly<HttpRequestException>()
            .WithMessage($"*{FileAddress}*")
            .WithInnerExceptionExactly<TaskCanceledException>();
    }

    /// <summary>
    /// Answers each request with <see cref="StatusCode"/> and <see cref="Body"/>, throws <see cref="SendException"/>, or,
    /// with <see cref="WaitsUntilCancelled"/>, waits until the client cancels the request. It supports only the
    /// synchronous send, so a test fails if the downloader sends asynchronously.
    /// </summary>
    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;
        public string Body { get; set; } = "";
        public Exception SendException { get; set; }
        public bool WaitsUntilCancelled { get; set; }
        public List<Uri> RequestUris { get; } = new();
        public DisposalTrackingResponse LastResponse { get; private set; }
        public ContentStream LastContentStream { get; private set; }

        protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestUris.Add(request.RequestUri);

            if (SendException != null)
            {
                throw SendException;
            }

            if (WaitsUntilCancelled)
            {
                // A limit, so that a test fails and does not hang if the client never cancels.
                cancellationToken.WaitHandle.WaitOne(TimeSpan.FromSeconds(30));
                cancellationToken.ThrowIfCancellationRequested();
                throw new InvalidOperationException("The client did not cancel the request.");
            }

            LastContentStream = new ContentStream(Encoding.UTF8.GetBytes(Body));
            LastResponse = new DisposalTrackingResponse(StatusCode)
            {
                Content = new StreamContent(LastContentStream),
                RequestMessage = request
            };
            return LastResponse;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            throw new NotSupportedException("The WMDA file downloader must send synchronously.");
        }
    }

    /// <summary>Records, when it is disposed, if all of its content was read.</summary>
    private sealed class ContentStream(byte[] content) : MemoryStream(content)
    {
        private bool isDisposed;

        public bool WasReadToEnd { get; private set; }

        protected override void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                WasReadToEnd = Position == Length;
                isDisposed = true;
            }

            base.Dispose(disposing);
        }
    }

    private sealed class DisposalTrackingResponse(HttpStatusCode statusCode) : HttpResponseMessage(statusCode)
    {
        public bool IsDisposed { get; private set; }

        protected override void Dispose(bool disposing)
        {
            IsDisposed = true;
            base.Dispose(disposing);
        }
    }
}
