using System.Collections.Generic;

namespace Atlas.HlaMetadataDictionary.Data
{
    internal interface IWmdaFileReader
    {
        IEnumerable<string> GetFileContentsWithoutHeader(string nomenclatureVersion, string fileName);
        string GetFirstNonCommentLine(string nomenclatureVersion, string fileName);
    }
}