using System.Collections.Generic;
using System.Linq;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.DonorImport.FileSchema.Models;
using Atlas.DonorImport.Test.TestHelpers.Models;
using Atlas.DonorImport.Test.TestHelpers.Models.MalformedDonorFileModels;
using AutoFixture.Dsl;

namespace Atlas.DonorImport.Test.TestHelpers.Builders;

internal static class DonorImportFileContentsBuilder
{
    public static IPostprocessComposer<SerialisableDonorImportFileContents> New => FixtureBuilder.For<SerialisableDonorImportFileContents>()
        .With(c => c.donors, new List<DonorUpdate>())
        .With(c => c.updateMode, UpdateMode.Differential);

    public static IPostprocessComposer<SerialisableDonorImportFileContents> WithDonorCount(
        this IPostprocessComposer<SerialisableDonorImportFileContents> builder,
        int numberOfDonors)
    {
        var donors = DonorUpdateBuilder.New.Build(numberOfDonors);
        return builder.With(c => c.donors, donors);
    }

    public static IPostprocessComposer<SerialisableDonorImportFileContents> WithDonors(
        this IPostprocessComposer<SerialisableDonorImportFileContents> builder,
        params DonorUpdate[] donors)
    {
        return builder.With(c => c.donors, donors);
    }

    public static IPostprocessComposer<SerialisableDonorImportFileContents> WithUpdateMode(
        this IPostprocessComposer<SerialisableDonorImportFileContents> builder,
        UpdateMode updateMode)
    {
        return builder.With(c => c.updateMode, updateMode);
    }
}

internal static class DonorImportFileWithNoDonorsBuilder
{
    public static IPostprocessComposer<DonorFileWithoutDonor> New => FixtureBuilder.For<DonorFileWithoutDonor>()
        .With(c => c.updateMode, UpdateMode.Differential);
}

internal static class DonorImportFileWithNoUpdateBuilder
{
    public static IPostprocessComposer<DonorFileWithoutUpdate> New => FixtureBuilder.For<DonorFileWithoutUpdate>()
        .With(c => c.donors, new List<DonorUpdate>());
}

internal static class DonorImportFileWithMissingFieldBuilder
{
    public static IPostprocessComposer<DonorFileWithDonorUpdateWithMissingField> New => FixtureBuilder.For<DonorFileWithDonorUpdateWithMissingField>()
        .With(c => c.donors, new List<DonorUpdateWithMissingField>())
        .With(c => c.updateMode, UpdateMode.Differential);

    public static IPostprocessComposer<DonorFileWithDonorUpdateWithMissingField> WithDonorCount(
        this IPostprocessComposer<DonorFileWithDonorUpdateWithMissingField> builder,
        int numberOfDonors)
    {
        var donors = DonorUpdateBuilder.New.Build(numberOfDonors).Select(x => new DonorUpdateWithMissingField(x));
        return builder.With(c => c.donors, donors);
    }
}

internal static class DonorImportFileWithInvalidEnumBuilder
{
    public static IPostprocessComposer<DonorFileWithDonorUpdateInvalidEnum> New => FixtureBuilder.For<DonorFileWithDonorUpdateInvalidEnum>()
        .With(c => c.donors, new List<DonorUpdateWithInvalidEnums>())
        .With(c => c.updateMode, "invalidEnum");

    public static IPostprocessComposer<DonorFileWithDonorUpdateInvalidEnum> WithInvalidEnumDonor(
        this IPostprocessComposer<DonorFileWithDonorUpdateInvalidEnum> builder)
    {
        var donor = DonorUpdateWithInvalidEnumBuilder.New.Build();
        return builder.With(c => c.donors, new[] {donor})
            .With(c => c.updateMode, UpdateMode.Differential.ToString());
    }

}