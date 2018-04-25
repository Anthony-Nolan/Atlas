using System.Collections.Generic;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;

namespace Nova.SearchAlgorithm.MatchingDictionary.Data.Wmda
{
    public interface IWmdaDataExtractor
    {
        IEnumerable<IWmdaHlaType> ExtractData(IWmdaRepository repo);
    }
}
