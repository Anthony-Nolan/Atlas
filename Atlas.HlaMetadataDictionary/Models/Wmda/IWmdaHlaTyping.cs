using Atlas.MatchingAlgorithm.MatchingDictionary.Models.HLATypings;

namespace Atlas.MatchingAlgorithm.MatchingDictionary.Models.Wmda
{
    public interface IWmdaHlaTyping
    {
        TypingMethod TypingMethod { get; }
        string TypingLocus { get; set; }
        string Name { get; set; }
    }
}
