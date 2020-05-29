using System;
using System.Collections.Generic;
using System.Linq;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;

namespace Atlas.MatchPrediction.Services.GenotypeLikelihood
{
    public interface IGenotypeImputer
    {
        public List<DiplotypeInfo<string>> GetPossibleDiplotypes(PhenotypeInfo<string> genotype);
    }

    public class GenotypeImputer : IGenotypeImputer
    {
        public List<DiplotypeInfo<string>> GetPossibleDiplotypes(PhenotypeInfo<string> genotype)
        {
            var diplotypes = new List<DiplotypeInfo<string>>();
            var genotypeLociInfo = new List<LocusInfo<string>>
            {
                genotype.A,
                genotype.B,
                genotype.C,
                genotype.Dqb1,
                genotype.Drb1
            };

            var heterozygousLoci = GetHeterozygousLoci(genotype);
            if (!heterozygousLoci.Any())
            {
                return new List<DiplotypeInfo<string>>{ new DiplotypeInfo<string>(genotype) };
            }

            // This method uses binary representations of i to indicate whether a particular locus should be swapped.
            // Each locus is assigned a bit, and the range of i guarantees that all permutations are considered.
            // Note that due to the symmetric nature of diplotypes (see DiplotypeInfo for further information), there are 2n-1 permutations, not 2n.
            for (var i = Math.Pow(2, heterozygousLoci.Count - 1); i < Math.Pow(2, heterozygousLoci.Count); i++)
            {
                var flags = Convert.ToString((int)i, 2).Select(c => c == '0').ToArray();

                var diplotype = new DiplotypeInfo<string>();

                for (var index = 0; index < heterozygousLoci.Count; index++)
                {
                    var locusInfo = GetLocusInfo(genotypeLociInfo[index], flags[index]);
                    diplotype.SetAtLocus(heterozygousLoci[index], locusInfo);
                }

                diplotypes.Add(diplotype);
            }

            return diplotypes;
        }

        private static LocusInfo<string> GetLocusInfo(LocusInfo<string> genotypeLocusInfo, bool shouldSwapPositions)
        {
            return shouldSwapPositions
                ? new LocusInfo<string> {Position1 = genotypeLocusInfo.Position2, Position2 = genotypeLocusInfo.Position1}
                : genotypeLocusInfo;
        }

        private static List<Locus> GetHeterozygousLoci(PhenotypeInfo<string> genotype)
        {
            var heterozygousLoci = new List<Locus>();

            genotype.EachLocus((locus, locusInfo) =>
            {
                if (locusInfo.Position1 != locusInfo.Position2 && locus != Locus.Dpb1)
                {
                    heterozygousLoci.Add(locus);
                }
            });

            return heterozygousLoci;
        }
    }
}