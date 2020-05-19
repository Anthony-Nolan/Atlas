using Atlas.Common.GeneticData.Hla.Models;

namespace Atlas.HlaMetadataDictionary.Models.Wmda
{
    internal interface IWmdaHlaTyping
    {
        TypingMethod TypingMethod { get; }
        string TypingLocus { get; set; }
        string Name { get; set; }
    }
}
