using System;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.DonorImport.FileSchema.Models;
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

        internal static Builder<ImportedHla> WithHomozygousMolecularHlaAtAllLoci(this Builder<ImportedHla> builder, string field) => builder.WithMolecularHlaAtAllLoci(field, field);
        internal static Builder<ImportedHla> WithHomozygousMolecularHlaAtLocus(this Builder<ImportedHla> builder, Locus locus, string field) => builder.WithMolecularHlaAtLocus(locus, field, field);
        internal static Builder<ImportedHla> WithValidHlaAtAllLoci(this Builder<ImportedHla> builder) => builder.WithMolecularHlaAtAllLoci("01:01", "01:01");
        
        internal static Builder<ImportedHla> WithMolecularHlaAtAllLoci(this Builder<ImportedHla> builder, string field1, string field2) =>
            builder
                .WithMolecularHlaAtLocus(Locus.A, field1, field2)
                .WithMolecularHlaAtLocus(Locus.B, field1, field2)
                .WithMolecularHlaAtLocus(Locus.C, field1, field2)
                .WithMolecularHlaAtLocus(Locus.Dpb1, field1, field2)
                .WithMolecularHlaAtLocus(Locus.Dqb1, field1, field2)
                .WithMolecularHlaAtLocus(Locus.Drb1, field1, field2);

        internal static Builder<ImportedHla> WithMolecularHlaAtLocus(this Builder<ImportedHla> builder, Locus locus, string field1, string field2)
        {
            return locus switch
            {
                Locus.A => builder.With(hla => hla.A, new ImportedLocus {Dna = new TwoFieldStringData {Field1 = field1, Field2 = field2}}),
                Locus.B => builder.With(hla => hla.B, new ImportedLocus {Dna = new TwoFieldStringData {Field1 = field1, Field2 = field2}}),
                Locus.C => builder.With(hla => hla.C, new ImportedLocus {Dna = new TwoFieldStringData {Field1 = field1, Field2 = field2}}),
                Locus.Dpb1 => builder.With(hla => hla.DPB1, new ImportedLocus {Dna = new TwoFieldStringData {Field1 = field1, Field2 = field2}}),
                Locus.Dqb1 => builder.With(hla => hla.DQB1, new ImportedLocus {Dna = new TwoFieldStringData {Field1 = field1, Field2 = field2}}),
                Locus.Drb1 => builder.With(hla => hla.DRB1, new ImportedLocus {Dna = new TwoFieldStringData {Field1 = field1, Field2 = field2}}),
                _ => throw new ArgumentOutOfRangeException(nameof(locus), locus, null)
            };
        }
    }
}