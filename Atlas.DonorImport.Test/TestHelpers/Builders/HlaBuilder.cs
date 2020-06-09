using Atlas.DonorImport.Models.FileSchema;
using LochNessBuilder;

namespace Atlas.DonorImport.Test.TestHelpers.Builders
{
    [Builder]
    internal static class HlaBuilder
    {
#pragma warning disable 618
        private static readonly ImportedLocus DefaultLocus = new ImportedLocus{ Dna = new TwoFieldStringData()};
#pragma warning restore 618
        
        internal static Builder<ImportedHla> New => Builder<ImportedHla>.New
            .With(hla => hla.A, DefaultLocus)
            .With(hla => hla.B, DefaultLocus)
            .With(hla => hla.C, DefaultLocus)
            .With(hla => hla.DPB1, DefaultLocus)
            .With(hla => hla.DQB1, DefaultLocus)
            .With(hla => hla.DRB1, DefaultLocus);
    }
}