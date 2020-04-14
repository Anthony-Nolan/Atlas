using System.Collections.Generic;

namespace Atlas.MatchingAlgorithm.MatchingDictionary.Data
{
    public interface IWmdaFileReader
    {
        IEnumerable<string> GetFileContentsWithoutHeader(string hlaDatabaseVersion, string fileName);
    }
}
