using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Atlas.MultipleAlleleCodeDictionary.Models;
using Atlas.MultipleAlleleCodeDictionary.Settings.MacImport;
using Microsoft.Extensions.Options;
using Polly;

namespace Atlas.MultipleAlleleCodeDictionary.utils
{
    public interface IMacParser
    {
        public Task<List<MultipleAlleleCodeEntity>> GetMacsSinceLastEntry(string lastMacEntry);
    }

    public class MacLineParser : IMacParser
    {
        private readonly IMacCodeDownloader macCodeDownloader;

        public MacLineParser(IMacCodeDownloader macCodeDownloader)
        {
            this.macCodeDownloader = macCodeDownloader;
        }

        public async Task<List<MultipleAlleleCodeEntity>> GetMacsSinceLastEntry(string lastMacEntry)
        {
            var macCodes = new List<MultipleAlleleCodeEntity>();

            await using var stream = await GetStream();
            using var reader = new StreamReader(stream);
            ReadToEntry(reader, lastMacEntry);

            while (!reader.EndOfStream)
            {
                var macLine = (await reader.ReadLineAsync())?.TrimEnd();

                if (string.IsNullOrWhiteSpace(macLine))
                {
                    continue;
                }

                macCodes.Add(ParseMac(macLine));
            }

            return macCodes;
        }

        private static MultipleAlleleCodeEntity ParseMac(string macString)
        {
            var substrings = macString.Split('\t');
            var isGeneric = substrings[0] != "*";
            return new MultipleAlleleCodeEntity(substrings[1], substrings[2], isGeneric);
        }

        private async Task<Stream> GetStream()
        {
            var retryPolicy = Policy.Handle<Exception>()
                .Retry(3);
            
            return await retryPolicy.Execute(async () => await macCodeDownloader.DownloadAndUnzipStream());
        }

        private static void ReadToEntry(StreamReader reader, string entryToReadTo)
        {
            // The first two lines of the NMDP source file contain descriptions, so are discarded
            reader.ReadLine();
            reader.ReadLine();
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine().TrimEnd();
                // Regex checking can slow down performance, it might be worth using a different comparison method if this slows us down significantly
                var match = Regex.IsMatch(line, $@"\b{entryToReadTo}\b");

                if (match)
                {
                    return;
                }
            }

            throw new Exception($"Reached end of file without finding entry {entryToReadTo}");
        }
    }
}