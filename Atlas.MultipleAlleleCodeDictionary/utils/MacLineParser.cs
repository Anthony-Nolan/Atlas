using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Atlas.MultipleAlleleCodeDictionary.Models;

namespace Atlas.MultipleAlleleCodeDictionary.utils
{
    public interface IMacParser
    {
        public List<MultipleAlleleCodeEntity> GetMacsSinceLastEntry(string lastMacEntry);
    }

    public class MacLineParser : IMacParser
    {
        private readonly IStreamProcessor streamProcessor;

        public MacLineParser()
        {
            const string url = "https://bioinformatics.bethematchclinical.org/HLA/alpha.v3.zip";
            streamProcessor = new StreamProcessor(url);
        }

        public List<MultipleAlleleCodeEntity> GetMacsSinceLastEntry(string lastMacEntry)
        {
            var macCodes = new List<MultipleAlleleCodeEntity>();

            using var stream = GetStream();
            using var reader = new StreamReader(stream);
            ReadToEntry(reader, lastMacEntry);

            while (!reader.EndOfStream)
            {
                var macLine = reader.ReadLine()?.TrimEnd();

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

        private Stream GetStream()
        {
            const int maxRetries = 3;
            var retries = 0;

            while (true)
            {
                try
                {
                    var stream = streamProcessor.DownloadAndUnzipStream();
                    return stream;
                }
                catch (Exception e)
                {
                    if (retries >= maxRetries)
                    {
                        throw;
                    }
                    retries++;
                }
            }
        }
        
        private static void ReadToEntry(StreamReader reader, string entryToReadTo)
        {
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine().TrimEnd();
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