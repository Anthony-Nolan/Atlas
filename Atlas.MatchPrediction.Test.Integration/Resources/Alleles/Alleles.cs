using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;

namespace Atlas.MatchPrediction.Test.Integration.Resources.Alleles
{
    internal static class Alleles
    {
        /// <summary>
        /// Alleles selected, such that each position has a single allele that corresponds to a single G-Group.
        /// All chosen alleles are heterozygous with respect to the chosen GGroup.
        /// Creates new property each time to avoid mutation.
        /// </summary>
        public static PhenotypeInfo<AlleleWithGGroup> UnambiguousAlleleDetails => new PhenotypeInfo<AlleleWithGGroup>
        (
            valueA: new LocusInfo<AlleleWithGGroup>
            (
                new AlleleWithGGroup {Allele = "02:09", GGroup = "02:01:01G"},
                new AlleleWithGGroup {Allele = "11:03", GGroup = "11:03:01G"}
            ),
            valueB: new LocusInfo<AlleleWithGGroup>
            (
                new AlleleWithGGroup {Allele = "15:12:01", GGroup = "15:12:01G"},
                new AlleleWithGGroup {Allele = "08:182", GGroup = "08:01:01G"}
            ),
            valueC: new LocusInfo<AlleleWithGGroup>
            (
                new AlleleWithGGroup {Allele = "01:03", GGroup = "01:03:01G"},
                new AlleleWithGGroup {Allele = "03:05", GGroup = "03:05:01G"}
            ),
            valueDqb1: new LocusInfo<AlleleWithGGroup>
            (
                new AlleleWithGGroup {Allele = "03:09", GGroup = "03:01:01G"},
                new AlleleWithGGroup {Allele = "02:04", GGroup = "02:01:01G"}
            ),
            valueDrb1: new LocusInfo<AlleleWithGGroup>
            (
                new AlleleWithGGroup {Allele = "03:124", GGroup = "03:01:01G"},
                new AlleleWithGGroup {Allele = "11:129", GGroup = "11:06:01G"}
            )
        );

        public static PhenotypeInfo<AlleleWithGGroup> AmbiguousAlleleDetails => new PhenotypeInfo<AlleleWithGGroup>
        (
            valueA: new LocusInfo<AlleleWithGGroup>
            (
                new AlleleWithGGroup {Allele = "01:01", GGroup = "01:01:01G"},
                new AlleleWithGGroup {Allele = "02:01", GGroup = "02:01:01G"}
            ),
            valueB: new LocusInfo<AlleleWithGGroup>
            (
                new AlleleWithGGroup {Allele = "15:146", GGroup = "15:01:01G"},
                new AlleleWithGGroup {Allele = "08:182", GGroup = "08:01:01G"}
            ),
            valueC: new LocusInfo<AlleleWithGGroup>
            (
                new AlleleWithGGroup {Allele = "04:82", GGroup = "04:01:01G"},
                new AlleleWithGGroup {Allele = "03:04", GGroup = "03:04:01G"}
            ),
            valueDqb1: new LocusInfo<AlleleWithGGroup>
            (
                new AlleleWithGGroup {Allele = "03:19", GGroup = "03:01:01G"},
                new AlleleWithGGroup {Allele = "03:03", GGroup = "03:03:01G"}
            ),
            valueDrb1: new LocusInfo<AlleleWithGGroup>
            (
                new AlleleWithGGroup {Allele = "*15:03", GGroup = "15:03:01G"},
                new AlleleWithGGroup {Allele = "*13:01", GGroup = "13:01:01G"}
            )
        );
    }

    internal class AlleleWithGGroup
    {
        public string Allele { get; set; }
        public string GGroup { get; set; }
    }

    internal static class AlleleWithGGroupPhenotypeExtensions
    {
        public static PhenotypeInfo<string> Alleles(this PhenotypeInfo<AlleleWithGGroup> phenotypeInfo)
        {
            return phenotypeInfo.Map((l, d) => l == Locus.Dpb1 ? null : d.Allele);
        }

        public static PhenotypeInfo<string> GGroups(this PhenotypeInfo<AlleleWithGGroup> phenotypeInfo)
        {
            return phenotypeInfo.Map((l, d) => l == Locus.Dpb1 ? null : d.GGroup);
        }
    }
}