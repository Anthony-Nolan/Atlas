using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Atlas.HlaMetadataDictionary.Data
{
    public class WmdaFileDownloader : IWmdaFileReader
    {
        private readonly string wmdaFileUri;

        public WmdaFileDownloader(string wmdaFileUri)
        {
            this.wmdaFileUri = wmdaFileUri;
        }
        
        public IEnumerable<string> GetFileContentsWithoutHeader(string hlaDatabaseVersion, string fileName)
        {
            return new WebClient()
                .DownloadString($"{wmdaFileUri}{hlaDatabaseVersion}/{fileName}")
                .Split('\n')
                .SkipWhile(line => line.StartsWith("#"));
        }
    }
}
