using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Atlas.MatchingAlgorithm.MatchingDictionary.Data;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.MatchingDictionary.Data
{
    public class WmdaTestFileImporter : IWmdaFileReader
    {
        private static readonly string TestDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        private const string FilePath = "/MatchingDictionary/Data/wmda-v";

        public IEnumerable<string> GetFileContentsWithoutHeader(string hlaDatabaseVersion, string fileName)
        {
            return File
                .ReadAllLines($"{TestDir}{FilePath}{hlaDatabaseVersion}/{fileName}")
                .SkipWhile(line => line.StartsWith("#"));
        }
    }
}