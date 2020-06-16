using Atlas.Common.GeneticData.Hla.Models.MolecularHlaTyping;
using Atlas.MultipleAlleleCodeDictionary.ExternalInterface.Models;
using LochNessBuilder;

namespace Atlas.MultipleAlleleCodeDictionary.Test.TestHelpers.Builders
{
    [Builder]
    internal class MolecularAlleleDetailsBuilder
    {
        public static Builder<MolecularAlleleDetails> New => Builder<MolecularAlleleDetails>.New;
    }
}