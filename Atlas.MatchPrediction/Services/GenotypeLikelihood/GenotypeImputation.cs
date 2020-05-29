using System;
using System.Collections.Generic;
using System.Linq;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;

namespace Atlas.MatchPrediction.Services.GenotypeLikelihood
{
    public interface IGenotypeImputation
    {
        public List<DiplotypeInfo<string>> GetPossibleDiplotypes(PhenotypeInfo<string> genotype);
    }

    public class GenotypeImputation : IGenotypeImputation
    {
        public List<DiplotypeInfo<string>> GetPossibleDiplotypes(PhenotypeInfo<string> genotype)
        {
            var diplotypes = new List<DiplotypeInfo<string>>();
            var genotypeLociInfo = genotype.ToEnumerableOfLoci().ToList();

            // Removes Dpb1 from list of locus info
            genotypeLociInfo.RemoveAt(3);

            var heterozygousLoci = GetHeterozygousLoci(genotype);
            if (!heterozygousLoci.Any())
            {
                return new List<DiplotypeInfo<string>>{ new DiplotypeInfo<string>(genotype) };
            }

            for (var i = Math.Pow(2, heterozygousLoci.Count - 1); i < Math.Pow(2, heterozygousLoci.Count); i++)
            {
                var flags = Convert.ToString((int)i, 2).Select(c => c == '0').ToArray();
                var index = 0;

                var diplotype = new DiplotypeInfo<string>();

                foreach (var locus in heterozygousLoci)
                {
                    var locusInfo = GetLocusInfo(genotypeLociInfo[index], flags[index]);
                    diplotype.SetAtLocus(locus, locusInfo);
                    index++;
                }

                diplotypes.Add(diplotype);
            }

            return diplotypes;
        }

        private static LocusInfo<string> GetLocusInfo(LocusInfo<string> genotypeLocusInfo, bool shouldSwapPositions)
        {
            return shouldSwapPositions
                ? new LocusInfo<string>() {Position1 = genotypeLocusInfo.Position2, Position2 = genotypeLocusInfo.Position1}
                : genotypeLocusInfo;
        }

        private static List<Locus> GetHeterozygousLoci(PhenotypeInfo<string> genotypeLociInfo)
        {
            var unusedLocus = new List<Locus>();

            genotypeLociInfo.EachLocus((locus, locusInfo) =>
            {
                if (locusInfo.Position1 != locusInfo.Position2)
                {
                    unusedLocus.Add(locus);
                }
            });

            return unusedLocus;
        }
    }
}