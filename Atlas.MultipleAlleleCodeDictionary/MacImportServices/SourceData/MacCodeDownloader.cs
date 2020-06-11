using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.MultipleAlleleCodeDictionary.Settings;
using Microsoft.Extensions.Options;

namespace Atlas.MultipleAlleleCodeDictionary.MacImportServices.SourceData
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
        private readonly ILogger logger;
        private readonly WebClient webClient = new WebClient();
        private readonly string url;

        public MacCodeDownloader(MacImportSettings macImportSettings, ILogger logger)
        {
            this.logger = logger;
            this.url = macImportSettings.MacSourceUrl;
        }
        
        /// <inheritdoc />
        public async Task<Stream> DownloadAndUnzipStream()
        {
            logger.SendTrace($"Downloading MACs from NMDP source", LogLevel.Info);
            var stream = await DownloadToMemoryStream();
            logger.SendTrace($"Downloaded MACs. Unzipping.", LogLevel.Info);
            return UnzipStream(stream);
        }

        private async Task<Stream> DownloadToMemoryStream()
        {
            byte[] data = await webClient.DownloadDataTaskAsync(url);
            var stream = new MemoryStream(data);
            return stream;
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