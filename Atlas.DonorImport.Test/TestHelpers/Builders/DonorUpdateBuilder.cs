using Atlas.DonorImport.Models.FileSchema;
using LochNessBuilder;

namespace Atlas.DonorImport.Test.TestHelpers.Builders
{
    [Builder]
    public static class DonorUpdateBuilder
    {
        public static Builder<DonorUpdate> New => Builder<DonorUpdate>.New
            .With(d => d.Hla, HlaBuilder.New.Build());
    }
}