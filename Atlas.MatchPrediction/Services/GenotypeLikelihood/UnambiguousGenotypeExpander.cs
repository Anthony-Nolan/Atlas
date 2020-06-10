using System;
using System.Collections.Generic;
using System.Linq;
using Atlas.MatchPrediction.Config;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.MatchPrediction.Models;

namespace Atlas.MatchPrediction.Services.GenotypeLikelihood
{
    public interface IUnambiguousGenotypeExpander
    {
        public ExpandedGenotype ExpandGenotype(PhenotypeInfo<string> genotype);
    }

    public class UnambiguousGenotypeExpander : IUnambiguousGenotypeExpander
    {
        public ExpandedGenotype ExpandGenotype(PhenotypeInfo<string> genotype)
        {
            var heterozygousLoci = GetHeterozygousLoci(genotype);
            if (!heterozygousLoci.Any())
            {
                return new ExpandedGenotype
                {
                    Diplotypes = new List<Diplotype> {new Diplotype(genotype)},
                    IsHomozygousAtEveryLocus = true
                };
            }

            var diplotypes = new List<Diplotype>();

            // This method uses binary representations of i to indicate whether the alleles of a particular locus should be swapped.
            // Each locus is assigned a bit, and the range of i guarantees that all permutations are considered.
            // Note that due to the symmetric nature of diplotypes (see DiplotypeInfo for further information), there are 2^(n-1) permutations, not 2n.
            for (var decimalRepresentationOfBinaryFlags = Math.Pow(2, heterozygousLoci.Count - 1);
                decimalRepresentationOfBinaryFlags < Math.Pow(2, heterozygousLoci.Count);
                decimalRepresentationOfBinaryFlags++)
            {
                var flags = Convert.ToString((int) decimalRepresentationOfBinaryFlags, 2)
                    .Select(c => c == '0').ToArray();

                var diplotype = new Diplotype(genotype);

                for (var i = 0; i < heterozygousLoci.Count; i++)
                {
                    var shouldSwapPositions = flags[i];
                    if (shouldSwapPositions)
                    {
                        var locus = heterozygousLoci[i];
                        var locusInfo = SwapLocus(genotype.GetLocus(locus));
                        diplotype.SetAtLocus(locus, locusInfo);
                    }
                }

                diplotypes.Add(diplotype);
            }

            return new ExpandedGenotype
            {
                Diplotypes = diplotypes,
                IsHomozygousAtEveryLocus = false
            };
        }

        private static LocusInfo<string> SwapLocus(LocusInfo<string> genotypeLocusInfo)
        {
            return new LocusInfo<string>
                {Position1 = genotypeLocusInfo.Position2, Position2 = genotypeLocusInfo.Position1};
        }

        private static List<Locus> GetHeterozygousLoci(PhenotypeInfo<string> genotype)
        {
            var heterozygousLoci = new List<Locus>();
            var allowedLoci = LocusSettings.MatchPredictionLoci.ToList();

            genotype.EachLocus((locus, locusInfo) =>
            {
                if (locusInfo.Position1 != locusInfo.Position2 && allowedLoci.Contains(locus))
                {
                    heterozygousLoci.Add(locus);
                }
            });

            return heterozygousLoci;
        }
    }
}