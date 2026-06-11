using System.Collections.Generic;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.HlaMetadataDictionary.InternalModels.HLATypings;
using AutoFixture.Dsl;

namespace Atlas.HlaMetadataDictionary.Test.TestHelpers.Builders;

internal static class SmallGGroupBuilder
{
    private const string DefaultName = "small-g-group";

    public static IPostprocessComposer<SmallGGroup> Default => FixtureBuilder.For<SmallGGroup>()
        .With(x => x.Locus, Locus.A)
        .With(x => x.Name, DefaultName)
        .With(x => x.Alleles, new List<string>());

    public static IPostprocessComposer<SmallGGroup> WithAllele(this IPostprocessComposer<SmallGGroup> builder, string allele)
    {
        return builder.With(x => x.Alleles, new[] { allele });
    }
}