using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypes;
using Nova.SearchAlgorithm.MatchingDictionary.Models.MatchingTypes;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.Dictionary
{
    /// <summary>
    /// Identifies classes that can serve as a data source
    /// for the creation of a matching dictionary entry.
    /// </summary>
    public interface IDictionarySource<out THlaType> : IMatchingPGroups, IMatchingSerologies where THlaType : HlaType
    {
        THlaType TypeForDictionary { get; }
    }
}
