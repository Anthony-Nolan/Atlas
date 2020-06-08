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
        public Task<Stream> DownloadAndUnzipStream();
    }

    internal class MacCodeDownloader : IMacCodeDownloader
    {
        private readonly ILogger logger;
        private readonly WebClient webClient = new WebClient();
        private readonly string url;

        public MacCodeDownloader(IOptions<MacImportSettings> macImportSettings, ILogger logger)
        {
            this.logger = logger;
            this.url = macImportSettings.Value.MacSourceUrl;
        }

        public async Task<Stream> DownloadAndUnzipStream()
        {
            logger.SendTrace($"Downloading MACs from NMDP source", LogLevel.Info);
            var stream = await DownloadToMemoryStream();
            logger.SendTrace($"Downloaded MACs. Unzipping.", LogLevel.Info);
            return UnzipStream(stream);
        }

        private async Task<Stream> DownloadToMemoryStream()
        {
            var data = await webClient.DownloadDataTaskAsync(url);
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