using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;

namespace Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda
{
    public interface IWmdaHlaTyping
    {
        TypingMethod TypingMethod { get; }
        string WmdaLocus { get; set; }
        string Name { get; set; }
    }
}
