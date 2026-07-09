using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Test.SharedTestHelpers;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.DonorImport.FileSchema.Models;
using Atlas.DonorImport.Test.TestHelpers.Models.MalformedDonorFileModels;
using AutoFixture.Dsl;

namespace Atlas.DonorImport.Test.TestHelpers.Builders;

internal static class DonorUpdateBuilder
{
    private const string RecordIdPrefix = "donor-update-";

    internal static IPostprocessComposer<DonorUpdate> New => FixtureBuilder.For<DonorUpdate>()
        .WithRecordIdPrefix(RecordIdPrefix)
        .With(d => d.Hla, HlaBuilder.Default.WithValidHlaAtAllLoci().Build())
        .With(d => d.UpdateMode, UpdateMode.Differential)
        .With(d => d.DonorType, ImportDonorType.Adult);

    internal static IPostprocessComposer<DonorUpdate> NoHla => FixtureBuilder.For<DonorUpdate>()
        .WithRecordIdPrefix(RecordIdPrefix)
        .With(d => d.UpdateMode, UpdateMode.Differential)
        .With(d => d.DonorType, ImportDonorType.Adult);

    internal static IPostprocessComposer<DonorUpdate> WithRecordIdPrefix(this IPostprocessComposer<DonorUpdate> builder, string recordIdPrefix)
    {
        return builder.With(d => d.RecordId, IncrementingIdGenerator.NextStringIdFactory(recordIdPrefix));
    }

    internal static IPostprocessComposer<DonorUpdate> WithHla(this IPostprocessComposer<DonorUpdate> builder, ImportedHla hla)
    {
        return builder.With(d => d.Hla, hla);
    }

    internal static IPostprocessComposer<DonorUpdate> WithHomozygousHlaAt(this IPostprocessComposer<DonorUpdate> builder, Locus locus, string hla)
    {
        return builder.With(d => d.Hla, HlaBuilder.Default.WithValidHlaAtAllLoci().WithHomozygousMolecularHlaAtLocus(locus, hla).Build());
    }
}

internal static class DonorUpdateWithInvalidEnumBuilder
{
    internal static IPostprocessComposer<DonorUpdateWithInvalidEnums> New => FixtureBuilder.For<DonorUpdateWithInvalidEnums>()
        .With(d => d.Hla, HlaBuilder.Default.Build())
        .With(d => d.UpdateMode, "INVALID")
        .With(d => d.DonorType, "INVALID");
}