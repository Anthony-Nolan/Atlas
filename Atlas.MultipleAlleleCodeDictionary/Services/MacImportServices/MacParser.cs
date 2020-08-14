using Atlas.Common.ApplicationInsights;
using Atlas.MultipleAlleleCodeDictionary.ExternalInterface.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Atlas.MultipleAlleleCodeDictionary.Services.MacImportServices
{
    internal interface IMacParser
    {
        public IAsyncEnumerable<Mac> GetMacsAsync(Stream file, string lastMacEntry);
        public Task<List<Mac>> GetMacsSince(Stream file, string lastMacEntry);
    }

    internal class MacLineParser : IMacParser
    {
        private readonly ILogger logger;

        public MacLineParser(ILogger logger)
        {
            this.logger = logger;
        }

        public async IAsyncEnumerable<Mac> GetMacsAsync(Stream file, string lastMacEntry)
        {
            logger.SendTrace($"Parsing MACs since: {lastMacEntry}");

            using (var reader = new StreamReader(file))
            {
                ReadToEntry(reader, lastMacEntry);
                while (!reader.EndOfStream)
                {
                    var macLine = (await reader.ReadLineAsync())?.TrimEnd();

                    if (string.IsNullOrWhiteSpace(macLine))
                    {
                        continue;
                    }

                    yield return ParseMac(macLine);
                }
            }
        }

        /// <inheritdoc />
        public async Task<List<Mac>> GetMacsSince(Stream file, string lastMacEntry)
        {
            logger.SendTrace($"Parsing MACs since: {lastMacEntry}");
            var macCodes = new List<Mac>();

            using (var reader = new StreamReader(file))
            {
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
            }

            return macCodes;
        }

        private static Mac ParseMac(string macString)
        {
            var substrings = macString.Split('\t');
            var isGeneric = substrings[0] != "*";
            return new Mac(substrings[1], substrings[2], isGeneric);
        }

        private static void ReadToEntry(StreamReader reader, string entryToReadTo = null)
        {
            // The first two lines of the NMDP source file contain descriptions, so are discarded
            reader.ReadLine();
            reader.ReadLine();

            if (entryToReadTo == null)
            {
                return;
            }
            
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