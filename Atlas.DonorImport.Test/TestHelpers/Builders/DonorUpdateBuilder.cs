using Atlas.DonorImport.Models.FileSchema;
using LochNessBuilder;

namespace Atlas.DonorImport.Test.TestHelpers.Builders
{
    [Builder]
    internal static class DonorUpdateBuilder
    {
        internal static Builder<DonorUpdate> New => Builder<DonorUpdate>.New
            .With(d => d.Hla, HlaBuilder.New.Build())
            .With(d => d.UpdateMode, UpdateMode.Differential);
    }
}