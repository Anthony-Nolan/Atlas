using Atlas.Common.GeneticData;
using Atlas.Common.Test.SharedTestHelpers;
using Atlas.DonorImport.Models.FileSchema;
using Atlas.DonorImport.Test.TestHelpers.Models.MalformedDonorFileModels;
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

        internal static Builder<DonorUpdate> WithHla(this Builder<DonorUpdate> builder, ImportedHla hla)
        {
            return builder.With(d => d.Hla, hla);
        }

        internal static Builder<DonorUpdate> WithHomozygousHlaAt(this Builder<DonorUpdate> builder, Locus locus, string hla)
        {
            return builder.With(d => d.Hla, HlaBuilder.New.WithHomozygousMolecularHlaAtLocus(locus, hla).Build());
        }
    }

    [Builder]
    internal static class DonorUpdateWithInvalidEnumBuilder
    {
        internal static Builder<DonorUpdateWithInvalidEnums> New => Builder<DonorUpdateWithInvalidEnums>.New
            .With(d => d.Hla, HlaBuilder.New.Build())
            .With(d => d.UpdateMode, "INVALID")
            .With(d => d.DonorType, "INVALID");
    }
}