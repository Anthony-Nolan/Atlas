using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nova.SearchAlgorithm.MatchingDictionary.Data;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Data
{
    public class WmdaTestFileImporter : IWmdaFileReader
    {
        private static readonly string TestDir = TestContext.CurrentContext.TestDirectory;
        private const string FilePath = "\\MatchingDictionary\\Data\\wmda-v";

        public IEnumerable<string> GetFileContentsWithoutHeader(string hlaDatabaseVersion, string fileName)
        {
            return File
                .ReadAllLines($"{TestDir}{FilePath}{hlaDatabaseVersion}\\{fileName}")
                .SkipWhile(line => line.StartsWith("#"));
        }
    }
}