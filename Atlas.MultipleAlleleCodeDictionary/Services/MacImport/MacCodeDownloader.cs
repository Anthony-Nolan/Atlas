﻿using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.MultipleAlleleCodeDictionary.Settings;
using Polly;

namespace Atlas.MultipleAlleleCodeDictionary.Services.MacImport
{
    internal interface IMacCodeDownloader
    {
        /// <remarks>
        ///  Downloads the zipped Mac Source file fully, and when that is complete, unzips the result into a stream.
        /// </remarks>
        public Task<Stream> DownloadAndUnzipStream();
    }

    internal class MacCodeDownloader : IMacCodeDownloader
    {
        /// <summary>The name of the <see cref="HttpClient"/> that downloads the MAC file.</summary>
        internal const string HttpClientName = nameof(MacCodeDownloader);

        /// <summary>
        /// The <see cref="HttpClient.Timeout"/> of the client: the time limit of a whole download, the content included.
        /// </summary>
        internal static readonly TimeSpan DownloadTimeout = TimeSpan.FromMinutes(5);

        /// <summary>The number of new attempts after a failed download or unzip.</summary>
        internal const int RetryCount = 3;

        private readonly IHttpClientFactory httpClientFactory;
        private readonly IAtlasLogger logger;
        private readonly string url;

        public MacCodeDownloader(IHttpClientFactory httpClientFactory, MacDownloadSettings macDownloadSettings, IAtlasLogger logger)
        {
            this.httpClientFactory = httpClientFactory;
            this.logger = logger;
            url = macDownloadSettings.MacSourceUrl;
        }

        /// <inheritdoc />
        public async Task<Stream> DownloadAndUnzipStream()
        {
            // An async policy, so that it awaits each attempt and sees its exception. A sync policy would only see the
            // returned task, and a failed attempt would reach the caller with no retry.
            var retryPolicy = Policy
                .Handle<Exception>()
                .RetryAsync(RetryCount, (exception, retryNumber) =>
                    logger.SendTrace(
                        $"MAC download or unzip failed, retry {retryNumber} of {RetryCount}. {exception.Message}",
                        LogLevel.Warn));

            return await retryPolicy.ExecuteAsync(AttemptToDownloadAndUnzipStream);
        }

        private async Task<Stream> AttemptToDownloadAndUnzipStream()
        {
            logger.SendTrace($"Downloading MACs from NMDP source");
            var stream = await DownloadToMemoryStream();
            logger.SendTrace($"Downloaded MACs. Unzipping.");
            return UnzipStream(stream);
        }

        /// <summary>
        /// Reads the whole response into memory before it returns, so the client's timeout covers the content too.
        /// </summary>
        private async Task<Stream> DownloadToMemoryStream()
        {
            // A new client for each download, as IHttpClientFactory advises, so it follows DNS changes.
            var httpClient = httpClientFactory.CreateClient(HttpClientName);

            try
            {
                using var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseContentRead);

                // Unlike WebClient, HttpClient does not throw on a non-success status. Without this check, the error page
                // would be unzipped as the MAC file.
                response.EnsureSuccessStatusCode();

                var data = await response.Content.ReadAsByteArrayAsync();
                return new MemoryStream(data);
            }
            // GetAsync gets no cancellation token, so a TaskCanceledException from it is always the timeout of the client.
            // The messages of a timeout and of a connection failure do not have the file address, so the address is added here.
            catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
            {
                throw new HttpRequestException(
                    $"Could not download the MAC file {url}. {exception.Message}",
                    exception,
                    (exception as HttpRequestException)?.StatusCode);
            }
        }

        private static Stream UnzipStream(Stream stream)
        {
            var zipArchive = new ZipArchive(stream);
            if (zipArchive.Entries.Count > 1)
            {
                throw new InvalidOperationException("NMDP zip archive contained more than one file");
            }

            var fileName = zipArchive.Entries.Single().FullName;
            var entry = zipArchive.GetEntry(fileName);
            var unzippedStream = entry?.Open();
            return unzippedStream;
        }
    }
}
