using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Atlas.HlaMetadataDictionary.WmdaDataAccess;

namespace Atlas.HlaMetadataDictionary.Test.UnitTests.Data
{
    public class WmdaTestFileImporter : IWmdaFileReader
    {
        private static readonly string TestDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        private const string FilePath = "/UnitTests/Data/wmda-v";

        public IEnumerable<string> GetFileContentsWithoutHeader(string hlaNomenclatureVersion, string fileName)
        {
            return File
                .ReadAllLines(FullFilePath(hlaNomenclatureVersion, fileName))
                .SkipWhile(IsCommentLine);
        }

        public string GetFirstNonCommentLine(string nomenclatureVersion, string fileName)
        {
            return File
                .ReadAllLines(FullFilePath(nomenclatureVersion, fileName))
                .SkipWhile(IsCommentLine)
                .First();
        }

        private static string FullFilePath(string hlaNomenclatureVersion, string fileName)
        {
            return $"{TestDir}{FilePath}{hlaNomenclatureVersion}/{fileName}";
        }

        private static bool IsCommentLine(string line)
        {
            return line.StartsWith("#");
        }
    }
}