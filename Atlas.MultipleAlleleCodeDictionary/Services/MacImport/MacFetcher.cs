using Atlas.MultipleAlleleCodeDictionary.ExternalInterface.Models;
using System.Collections.Generic;

namespace Atlas.MultipleAlleleCodeDictionary.Services.MacImport
{
    internal interface IMacFetcher
    {
        IAsyncEnumerable<Mac> FetchAndLazilyParseMacsSince(string lastMacEntry);
    }

    internal class MacFetcher : IMacFetcher
    {
        private readonly IMacCodeDownloader macCodeDownloader;
        private readonly IMacParser macParser;

        public MacFetcher(IMacCodeDownloader macCodeDownloader, IMacParser macParser)
        {
            this.macCodeDownloader = macCodeDownloader;
            this.macParser = macParser;
        }

        public async IAsyncEnumerable<Mac> FetchAndLazilyParseMacsSince(string lastMacEntry)
        {
            await using var macStream = await macCodeDownloader.DownloadAndUnzipStream();
            await foreach (var mac in macParser.ParseMacsSince(macStream, lastMacEntry))
            {
                yield return mac;
            }
        }
    }
}