using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;

namespace Nova.SearchAlgorithm.MatchingDictionary.Data
{
    public class WmdaFileDownloader : IWmdaFileReader
    {
        private static readonly string WmdaFileUri = ConfigurationManager.ConnectionStrings["WmdaFileUri"].ConnectionString;

        public IEnumerable<string> GetFileContentsWithoutHeader(string hlaDatabaseVersion, string fileName)
        {
            return new WebClient()
                .DownloadString($"{WmdaFileUri}{hlaDatabaseVersion}/{fileName}")
                .Split('\n')
                .SkipWhile(line => line.StartsWith("#"));
        }
    }
}
