using Atlas.DonorImport.Models.FileSchema;
using LochNessBuilder;

namespace Atlas.DonorImport.Test.TestHelpers.Builders
{
    [Builder]
    internal static class HlaBuilder
    {
        internal static Builder<Hla> New => Builder<Hla>.New
            .With(hla => hla.A, new Locus())
            .With(hla => hla.B, new Locus())
            .With(hla => hla.C, new Locus())
            .With(hla => hla.DPB1, new Locus())
            .With(hla => hla.DQB1, new Locus())
            .With(hla => hla.DRB1, new Locus());
    }
}