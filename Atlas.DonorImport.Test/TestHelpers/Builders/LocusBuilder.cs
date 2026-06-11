using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.DonorImport.FileSchema.Models;
using AutoFixture.Dsl;

namespace Atlas.DonorImport.Test.TestHelpers.Builders;

internal static class LocusBuilder
{
    private const string DefaultDnaTyping = "01:01";
    private const string DefaultSerologyTyping = "1";

    internal static IPostprocessComposer<ImportedLocus> Default => FixtureBuilder.For<ImportedLocus>()
        .WithDna(DefaultDnaTyping, DefaultDnaTyping)
        .WithSerology(DefaultSerologyTyping, DefaultSerologyTyping);

    internal static IPostprocessComposer<ImportedLocus> WithDna(this IPostprocessComposer<ImportedLocus> builder, string field1, string field2)
    {
        return builder.WithDna(BuildTwoFieldTyping(field1, field2));
    }

    internal static IPostprocessComposer<ImportedLocus> WithDna(this IPostprocessComposer<ImportedLocus> builder, TwoFieldStringData data)
    {
        return builder.With(x => x.Dna, data);
    }

    internal static IPostprocessComposer<ImportedLocus> WithSerology(this IPostprocessComposer<ImportedLocus> builder, string field1, string field2)
    {
        return builder.WithSerology(BuildTwoFieldTyping(field1, field2));
    }

    internal static IPostprocessComposer<ImportedLocus> WithSerology(this IPostprocessComposer<ImportedLocus> builder, TwoFieldStringData data)
    {
        return builder.With(x => x.Serology, data);
    }

    private static TwoFieldStringData BuildTwoFieldTyping(string field1, string field2)
    {
        return new TwoFieldStringData
        {
            Field1 = field1,
            Field2 = field2
        };
    }
}