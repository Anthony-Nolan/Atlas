using System.Collections.Generic;

namespace Atlas.HlaMetadataDictionary.Data
{
    public interface IWmdaFileReader
    {
        IEnumerable<string> GetFileContentsWithoutHeader(string hlaDatabaseVersion, string fileName);
    }
}
