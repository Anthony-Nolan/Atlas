using Atlas.Common.GeneticData.Hla.Models;

namespace Atlas.HlaMetadataDictionary.Models.Wmda
{
    public interface IWmdaHlaTyping
    {
        TypingMethod TypingMethod { get; }
        string TypingLocus { get; set; }
        string Name { get; set; }
    }
}
