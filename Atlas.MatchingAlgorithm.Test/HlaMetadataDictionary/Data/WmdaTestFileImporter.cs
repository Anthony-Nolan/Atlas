using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Atlas.HlaMetadataDictionary.Data;

namespace Atlas.MatchingAlgorithm.Test.HlaMetadataDictionary.Data
{
    public class WmdaTestFileImporter : IWmdaFileReader
    {
        private static readonly string TestDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        private const string FilePath = "/HlaMetadataDictionary/Data/wmda-v";

        public IEnumerable<string> GetFileContentsWithoutHeader(string hlaDatabaseVersion, string fileName)
        {
            return File
                .ReadAllLines($"{TestDir}{FilePath}{hlaDatabaseVersion}/{fileName}")
                .SkipWhile(line => line.StartsWith("#"));
        }
    }
}