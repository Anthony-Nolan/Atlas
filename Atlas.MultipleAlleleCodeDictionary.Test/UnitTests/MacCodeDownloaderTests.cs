using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.MultipleAlleleCodeDictionary.Services.MacImport;
using Atlas.MultipleAlleleCodeDictionary.Settings;
using AutoFixture;
using AwesomeAssertions;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.MultipleAlleleCodeDictionary.Test.UnitTests;

[TestFixture]
internal class MacCodeDownloaderTests
{
    private Fixture fixture;
    private FakeHttpMessageHandler handler;
    private string sourceUrl;
    private TimeSpan clientTimeout;
    private MacCodeDownloader downloader;

    [SetUp]
    public void SetUp()
    {
        fixture = new Fixture();
        handler = new FakeHttpMessageHandler();
        sourceUrl = fixture.Create<Uri>().ToString();
        clientTimeout = MacCodeDownloader.DownloadTimeout;

        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        httpClientFactory.CreateClient(MacCodeDownloader.HttpClientName)
            .Returns(_ => new HttpClient(handler, disposeHandler: false) { Timeout = clientTimeout });

        downloader = new MacCodeDownloader(
            httpClientFactory,
            new MacDownloadSettings { MacSourceUrl = sourceUrl },
            Substitute.For<IAtlasLogger>());
    }

    [TearDown]
    public void TearDown()
    {
        handler.Dispose();
    }

    private const int AllAttempts = MacCodeDownloader.RetryCount + 1;

    [Test]
    public async Task DownloadAndUnzipStream_RequestsSourceUrl()
    {
        handler.Responses.Enqueue(Success(Zip(fixture.Create<string>())));

        await downloader.DownloadAndUnzipStream();

        handler.RequestUris.Should().ContainSingle().Which.Should().Be(new Uri(sourceUrl));
    }

    [Test]
    public async Task DownloadAndUnzipStream_WhenZipHasOneEntry_ReturnsUnzippedContent()
    {
        var macFileContent = string.Join('\n', fixture.CreateMany<string>(100));
        handler.Responses.Enqueue(Success(Zip(macFileContent)));

        await using var stream = await downloader.DownloadAndUnzipStream();

        (await ReadToEnd(stream)).Should().Be(macFileContent);
    }

    [Test]
    public async Task DownloadAndUnzipStream_ReadsWholeDownloadBeforeItReturns()
    {
        handler.Responses.Enqueue(Success(Zip(fixture.Create<string>())));

        await downloader.DownloadAndUnzipStream();

        handler.LastResponse.IsDisposed.Should().BeTrue();
        handler.LastContentStream.WasReadToEnd.Should().BeTrue();
    }

    [Test]
    public async Task DownloadAndUnzipStream_WhenZipHasMoreThanOneEntry_ThrowsAfterAllAttempts()
    {
        for (var i = 0; i < AllAttempts; i++)
        {
            handler.Responses.Enqueue(Success(Zip(fixture.Create<string>(), fixture.Create<string>())));
        }

        var act = () => downloader.DownloadAndUnzipStream();

        await act.Should().ThrowExactlyAsync<InvalidOperationException>().WithMessage("*more than one file*");
        handler.RequestUris.Should().HaveCount(AllAttempts);
    }

    [TestCase(HttpStatusCode.NotFound)]
    [TestCase(HttpStatusCode.InternalServerError)]
    public async Task DownloadAndUnzipStream_WhenStatusIsNotSuccess_ThrowsWithFileAddress(HttpStatusCode statusCode)
    {
        handler.DefaultStatusCode = statusCode;

        var act = () => downloader.DownloadAndUnzipStream();

        (await act.Should().ThrowExactlyAsync<HttpRequestException>().WithMessage($"*{sourceUrl}*"))
            .Which.StatusCode.Should().Be(statusCode);
        handler.LastResponse.IsDisposed.Should().BeTrue();
    }

    [Test]
    public async Task DownloadAndUnzipStream_WhenSendFails_ThrowsWithFileAddress()
    {
        var sendException = new HttpRequestException(fixture.Create<string>());
        handler.SendException = sendException;

        var act = () => downloader.DownloadAndUnzipStream();

        (await act.Should().ThrowExactlyAsync<HttpRequestException>().WithMessage($"*{sourceUrl}*"))
            .Which.InnerException.Should().BeSameAs(sendException);
    }

    [Test]
    public async Task DownloadAndUnzipStream_WhenClientTimesOut_ThrowsWithFileAddress()
    {
        clientTimeout = TimeSpan.FromMilliseconds(50);
        handler.WaitsUntilCancelled = true;

        var act = () => downloader.DownloadAndUnzipStream();

        await act.Should().ThrowExactlyAsync<HttpRequestException>()
            .WithMessage($"*{sourceUrl}*")
            .WithInnerExceptionExactly(typeof(TaskCanceledException));
    }

    [Test]
    public async Task DownloadAndUnzipStream_WhenFirstDownloadFails_RetriesAndReturnsUnzippedContent()
    {
        var macFileContent = fixture.Create<string>();
        handler.Responses.Enqueue(new FakeResponse(HttpStatusCode.ServiceUnavailable, []));
        handler.Responses.Enqueue(Success(Zip(macFileContent)));

        await using var stream = await downloader.DownloadAndUnzipStream();

        (await ReadToEnd(stream)).Should().Be(macFileContent);
        handler.RequestUris.Should().HaveCount(2);
    }

    [Test]
    public async Task DownloadAndUnzipStream_WhenFirstUnzipFails_RetriesAndReturnsUnzippedContent()
    {
        var macFileContent = fixture.Create<string>();
        handler.Responses.Enqueue(Success(Encoding.UTF8.GetBytes("not a zip")));
        handler.Responses.Enqueue(Success(Zip(macFileContent)));

        await using var stream = await downloader.DownloadAndUnzipStream();

        (await ReadToEnd(stream)).Should().Be(macFileContent);
        handler.RequestUris.Should().HaveCount(2);
    }

    [Test]
    public async Task DownloadAndUnzipStream_WhenEveryAttemptFails_TriesFourTimesThenThrows()
    {
        handler.DefaultStatusCode = HttpStatusCode.NotFound;

        var act = () => downloader.DownloadAndUnzipStream();

        await act.Should().ThrowExactlyAsync<HttpRequestException>();
        handler.RequestUris.Should().HaveCount(AllAttempts);
    }

    private static FakeResponse Success(byte[] body) => new(HttpStatusCode.OK, body);

    private static byte[] Zip(params string[] entryContents)
    {
        using var zipStream = new MemoryStream();
        using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            for (var i = 0; i < entryContents.Length; i++)
            {
                using var writer = new StreamWriter(archive.CreateEntry($"entry{i}.txt").Open());
                writer.Write(entryContents[i]);
            }
        }

        return zipStream.ToArray();
    }

    private static async Task<string> ReadToEnd(Stream stream)
    {
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }

    private sealed record FakeResponse(HttpStatusCode StatusCode, byte[] Body);

    /// <summary>
    /// Answers each request with the next of <see cref="Responses"/>, or, when there are none left, with
    /// <see cref="DefaultStatusCode"/> and no body. Throws <see cref="SendException"/>, or, with
    /// <see cref="WaitsUntilCancelled"/>, waits until the client cancels the request.
    /// </summary>
    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        public Queue<FakeResponse> Responses { get; } = new();
        public HttpStatusCode DefaultStatusCode { get; set; } = HttpStatusCode.OK;
        public Exception SendException { get; set; }
        public bool WaitsUntilCancelled { get; set; }
        public List<Uri> RequestUris { get; } = new();
        public DisposalTrackingResponse LastResponse { get; private set; }
        public ContentStream LastContentStream { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestUris.Add(request.RequestUri);

            if (SendException != null)
            {
                throw SendException;
            }

            if (WaitsUntilCancelled)
            {
                // A limit, so that a test fails and does not hang if the client never cancels.
                await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
                throw new InvalidOperationException("The client did not cancel the request.");
            }

            var response = Responses.TryDequeue(out var next) ? next : new FakeResponse(DefaultStatusCode, []);
            LastContentStream = new ContentStream(response.Body);
            LastResponse = new DisposalTrackingResponse(response.StatusCode)
            {
                Content = new StreamContent(LastContentStream),
                RequestMessage = request
            };
            return LastResponse;
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
