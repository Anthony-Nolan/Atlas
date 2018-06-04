using Nova.SearchAlgorithm.MatchingDictionary.Data;
using NUnit.Framework;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;

namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Data
{
    public class WmdaTestFileImporter : IWmdaFileReader
    {
        private static readonly string TestDir = TestContext.CurrentContext.TestDirectory;
        private static readonly string FilePath = ConfigurationManager.ConnectionStrings["TestWmdaFilePath"].ConnectionString;

        public IEnumerable<string> GetFileContentsWithoutHeader(string fileName)
        {           
            return File
                .ReadAllLines($"{TestDir}{FilePath}{fileName}.txt")
                .SkipWhile(line => line.StartsWith("#"));
        }
    }
}
