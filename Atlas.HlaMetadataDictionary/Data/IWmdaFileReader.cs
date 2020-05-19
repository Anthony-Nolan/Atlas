using System.Collections.Generic;

namespace Atlas.HlaMetadataDictionary.Data
{
    internal interface IWmdaFileReader
    {
        IEnumerable<string> GetFileContentsWithoutHeader(string hlaDatabaseVersion, string fileName);
    }
}
