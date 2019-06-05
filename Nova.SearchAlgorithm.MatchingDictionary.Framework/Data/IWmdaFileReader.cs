using System.Collections.Generic;

namespace Nova.SearchAlgorithm.MatchingDictionary.Data
{
    public interface IWmdaFileReader
    {
        IEnumerable<string> GetFileContentsWithoutHeader(string hlaDatabaseVersion, string fileName);
    }
}
