using Atlas.Common.Test.SharedTestHelpers;
using Atlas.DonorImport.Models.FileSchema;
using LochNessBuilder;

namespace Atlas.DonorImport.Test.TestHelpers.Builders
{
    [Builder]
    internal static class DonorUpdateBuilder
    {
        internal static Builder<DonorUpdate> New => Builder<DonorUpdate>.New
            .WithRecordIdPrefix("donor-update-")
            .With(d => d.Hla, HlaBuilder.New.Build())
            .With(d => d.UpdateMode, UpdateMode.Differential)
            .With(d => d.DonorType, ImportDonorType.Adult);

        internal static Builder<DonorUpdate> WithRecordIdPrefix(this Builder<DonorUpdate> builder, string recordIdPrefix)
        {
            return builder.WithFactory(d => d.RecordId, IncrementingIdGenerator.NextStringIdFactory(recordIdPrefix));
        }
    }
}