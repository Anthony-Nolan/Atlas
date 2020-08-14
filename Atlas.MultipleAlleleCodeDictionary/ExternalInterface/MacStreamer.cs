using Atlas.MultipleAlleleCodeDictionary.ExternalInterface.Models;
using Atlas.MultipleAlleleCodeDictionary.Services.MacImportServices;
using Polly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Atlas.MultipleAlleleCodeDictionary.ExternalInterface
{
    public interface IMacStreamer
    {
        Task<IAsyncEnumerable<Mac>> StreamLatestMacsAsync(string lastMacEntry);
    }

    internal class MacStreamer : IMacStreamer
    {
        private readonly IMacCodeDownloader macCodeDownloader;
        private readonly IMacParser macParser;

        public MacStreamer(IMacCodeDownloader macCodeDownloader, IMacParser macParser)
        {
            this.macCodeDownloader = macCodeDownloader;
            this.macParser = macParser;
        }

        public async Task<IAsyncEnumerable<Mac>> StreamLatestMacsAsync(string lastMacEntry)
        {
            // do not wrap in `using` statement else stream will be disposed before being read!
            var macStream = await DownloadMacs();
            return macParser.GetMacsAsync(macStream, lastMacEntry);
        }

        private async Task<Stream> DownloadMacs()
        {
            var retryPolicy = Policy.Handle<Exception>().Retry(3);
            return await retryPolicy.Execute(async () => await macCodeDownloader.DownloadAndUnzipStream());
        }
    }
}