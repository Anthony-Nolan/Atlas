using Atlas.Common.GeneticData.Hla.Models.MolecularHlaTyping;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using AutoFixture.Dsl;

namespace Atlas.MultipleAlleleCodeDictionary.Test.TestHelpers.Builders;

internal class MolecularAlleleDetailsBuilder
{
    public static IPostprocessComposer<MolecularAlleleDetails> New => FixtureBuilder.For<MolecularAlleleDetails>();
}