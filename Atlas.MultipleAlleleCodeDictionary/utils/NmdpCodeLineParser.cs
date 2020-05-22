using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Atlas.MultipleAlleleCodeDictionary.Models;

namespace Atlas.MultipleAlleleCodeDictionary.utils
{
    public interface INmdpCodeParser
    {
        public List<MacCode> ParseNmdpCodeLinesToModelSet(string lastNmdpCodeEntry);
    }

    public class NmdpCodeLineParser : INmdpCodeParser
    {
        private readonly IStreamProcessor streamProcessor;

        public NmdpCodeLineParser()
        {
            const string url = "https://bioinformatics.bethematchclinical.org/HLA/alpha.v3.zip";
            streamProcessor = new StreamProcessor(url);
        }

        public List<MacCode> ParseNmdpCodeLinesToModelSet(string lastNmdpCodeEntry)
        {
            var macCodes = new List<MacCode>();

            using var stream = GetNmdpCodeStream();
            using var reader = new StreamReader(stream);
            ReadToEntry(reader, lastNmdpCodeEntry);

            while (!reader.EndOfStream)
            {
                var nmdpCodeLine = reader.ReadLine()?.TrimEnd();

                if (string.IsNullOrWhiteSpace(nmdpCodeLine))
                {
                    continue;
                }
                
                macCodes.Add(ParseMac(nmdpCodeLine)); 
            }

            return macCodes;
        }

        private static MacCode ParseMac(string macString)
        {
            var substrings = macString.Split('\t');
            var isGeneric = substrings[0] != "*";
            return new MacCode(substrings[1], substrings[2], isGeneric);

        }

        private Stream GetNmdpCodeStream()
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