using System;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.DonorImport.FileSchema.Models;
using AutoFixture.Dsl;

namespace Atlas.DonorImport.Test.TestHelpers.Builders;

internal static class HlaBuilder
{
    private static readonly ImportedLocus DefaultLocus = new() { Dna = new TwoFieldStringData()};

    internal static IPostprocessComposer<ImportedHla> New => FixtureBuilder.For<ImportedHla>();

    internal static IPostprocessComposer<ImportedHla> Default => New
        .With(hla => hla.A, DefaultLocus)
        .With(hla => hla.B, DefaultLocus)
        .With(hla => hla.C, DefaultLocus)
        .With(hla => hla.DPB1, DefaultLocus)
        .With(hla => hla.DQB1, DefaultLocus)
        .With(hla => hla.DRB1, DefaultLocus);

    internal static IPostprocessComposer<ImportedHla> WithHomozygousMolecularHlaAtAllLoci(this IPostprocessComposer<ImportedHla> builder, string field) => builder.WithMolecularHlaAtAllLoci(field, field);
    internal static IPostprocessComposer<ImportedHla> WithHomozygousMolecularHlaAtLocus(this IPostprocessComposer<ImportedHla> builder, Locus locus, string field) => builder.WithMolecularHlaAtLocus(locus, field, field);
    internal static IPostprocessComposer<ImportedHla> WithValidHlaAtAllLoci(this IPostprocessComposer<ImportedHla> builder) => builder.WithMolecularHlaAtAllLoci("01:01", "01:01");

    internal static IPostprocessComposer<ImportedHla> WithMolecularHlaAtAllLoci(this IPostprocessComposer<ImportedHla> builder, string field1, string field2) =>
        builder
            .WithMolecularHlaAtLocus(Locus.A, field1, field2)
            .WithMolecularHlaAtLocus(Locus.B, field1, field2)
            .WithMolecularHlaAtLocus(Locus.C, field1, field2)
            .WithMolecularHlaAtLocus(Locus.Dpb1, field1, field2)
            .WithMolecularHlaAtLocus(Locus.Dqb1, field1, field2)
            .WithMolecularHlaAtLocus(Locus.Drb1, field1, field2);

    internal static IPostprocessComposer<ImportedHla> WithMolecularHlaAtLocus(this IPostprocessComposer<ImportedHla> builder, Locus locus, string field1, string field2)
    {
        return builder.WithImportedLocus(locus, new ImportedLocus { Dna = new TwoFieldStringData { Field1 = field1, Field2 = field2 } });
    }

    internal static IPostprocessComposer<ImportedHla> WithImportedLocus(this IPostprocessComposer<ImportedHla> builder, Locus locus, ImportedLocus typing)
    {
        return locus switch
        {
            Locus.A => builder.With(x => x.A, typing),
            Locus.B => builder.With(x => x.B, typing),
            Locus.C => builder.With(x => x.C, typing),
            Locus.Dpb1 => builder.With(x => x.DPB1, typing),
            Locus.Dqb1 => builder.With(x => x.DQB1, typing),
            Locus.Drb1 => builder.With(x => x.DRB1, typing),
            _ => throw new ArgumentOutOfRangeException(nameof(locus), locus, null)
        };
    }
}