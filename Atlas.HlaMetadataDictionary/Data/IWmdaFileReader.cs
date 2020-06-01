using System.Collections.Generic;

namespace Atlas.HlaMetadataDictionary.Data
{
    internal interface IWmdaFileReader
    {
        IEnumerable<string> GetFileContentsWithoutHeader(string hlaNomenclatureVersion, string fileName);
        string GetFirstNonCommentLine(string nomenclatureVersion, string fileName);
    }
}