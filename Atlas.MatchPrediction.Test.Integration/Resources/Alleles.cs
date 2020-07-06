using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;

namespace Atlas.MatchPrediction.Test.Integration.Resources
{
    internal static class Alleles
    {
        /// <summary>
        /// Alleles selected, such that each position has a single allele that corresponds to a single G-Group.
        /// All chosen alleles are heterozygous with respect to the chosen GGroup
        /// </summary>
        public static readonly PhenotypeInfo<AlleleWithGGroup> UnambiguousAlleleDetails = new PhenotypeInfo<AlleleWithGGroup>
        {
            A = new LocusInfo<AlleleWithGGroup>
            {
                Position1 = new AlleleWithGGroup {Allele = "02:09", GGroup = "02:01:01G"},
                Position2 = new AlleleWithGGroup {Allele = "11:03", GGroup = "11:03:01G"}
            },
            B = new LocusInfo<AlleleWithGGroup>
            {
                Position1 = new AlleleWithGGroup {Allele = "15:12:01", GGroup = "15:12:01G"},
                Position2 = new AlleleWithGGroup {Allele = "08:182", GGroup = "08:01:01G"}
            },
            C = new LocusInfo<AlleleWithGGroup>
            {
                Position1 = new AlleleWithGGroup {Allele = "01:03", GGroup = "01:03:01G"},
                Position2 = new AlleleWithGGroup {Allele = "03:05", GGroup = "03:05:01G"}
            },
            Dqb1 = new LocusInfo<AlleleWithGGroup>
            {
                Position1 = new AlleleWithGGroup {Allele = "03:09", GGroup = "03:01:01G"},
                Position2 = new AlleleWithGGroup {Allele = "02:04", GGroup = "02:01:01G"}
            },
            Drb1 = new LocusInfo<AlleleWithGGroup>
            {
                Position1 = new AlleleWithGGroup {Allele = "03:124", GGroup = "03:01:01G"},
                Position2 = new AlleleWithGGroup {Allele = "11:129", GGroup = "11:06:01G"}
            }
        };

        public static PhenotypeInfo<string> UnambiguousAlleles => UnambiguousAlleleDetails.Map((l, d) => l == Locus.Dpb1 ? null : d.Allele);
        public static PhenotypeInfo<string> UnambiguousAllelesGGroups => UnambiguousAlleleDetails.Map((l, d) => l == Locus.Dpb1 ? null : d.GGroup);
    }

    internal class AlleleWithGGroup
    {
        public string Allele { get; set; }
        public string GGroup { get; set; }
    }
}