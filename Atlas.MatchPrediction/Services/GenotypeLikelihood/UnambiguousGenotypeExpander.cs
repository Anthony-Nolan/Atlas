using System;
using System.Collections.Generic;
using System.Linq;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.MatchPrediction.Models;

namespace Atlas.MatchPrediction.Services.GenotypeLikelihood
{
    internal interface IUnambiguousGenotypeExpander
    {
        public ExpandedGenotype ExpandGenotype(PhenotypeInfo<string> genotype, ISet<Locus> allowedLoci);
    }

    internal class UnambiguousGenotypeExpander : IUnambiguousGenotypeExpander
    {
        public ExpandedGenotype ExpandGenotype(PhenotypeInfo<string> genotype, ISet<Locus> allowedLoci)
        {
            var heterozygousLoci = GetHeterozygousLoci(genotype, allowedLoci);
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

        private static LocusInfo<string> SwapLocus(LocusInfo<string> genotypeLocusInfo) =>
            new LocusInfo<string>(genotypeLocusInfo.Position2, genotypeLocusInfo.Position1);

        private static List<Locus> GetHeterozygousLoci(PhenotypeInfo<string> genotype, ISet<Locus> allowedLoci)
        {
            var heterozygousLoci = new List<Locus>();

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