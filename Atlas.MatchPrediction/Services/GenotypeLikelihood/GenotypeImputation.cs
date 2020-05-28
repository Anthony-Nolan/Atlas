using System;
using System.Collections.Generic;
using System.Linq;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;

namespace Atlas.MatchPrediction.Services.GenotypeLikelihood
{
    public interface IGenotypeImputation
    {
        public List<PhenotypeInfo<string>> GetPossibleDiplotypes(PhenotypeInfo<string> genotype);
    }

    public class GenotypeImputation : IGenotypeImputation
    {
        public List<PhenotypeInfo<string>> GetPossibleDiplotypes(PhenotypeInfo<string> genotype)
        {
            var diplotypes = new List<PhenotypeInfo<string>>();

            var heterozygousLocus = GetHeterozygousLocusTypes(genotype);
            if (!heterozygousLocus.Any())
            {
                return new List<PhenotypeInfo<string>>{ genotype };
            }

            var locusCount = heterozygousLocus.Count();
            for (var i = Math.Pow(2, locusCount - 1); i < Math.Pow(2, locusCount); i++)
            {
                var flags = Convert.ToString((int)i, 2).Select(c => c == '1').ToArray();
                var flagValue = 0;

                var diplotype = new PhenotypeInfo<string>
                {
                    A = heterozygousLocus.Contains(Locus.A) ? GetLocusInfo(genotype.A, flags[flagValue++]) : genotype.A,
                    B = heterozygousLocus.Contains(Locus.B) ? GetLocusInfo(genotype.B, flags[flagValue++]) : genotype.B,
                    C = heterozygousLocus.Contains(Locus.C) ? GetLocusInfo(genotype.C, flags[flagValue++]) : genotype.C,
                    Dqb1 = heterozygousLocus.Contains(Locus.Dqb1) ? GetLocusInfo(genotype.Dqb1, flags[flagValue++]) : genotype.Dqb1,
                    Drb1 = heterozygousLocus.Contains(Locus.Drb1) ? GetLocusInfo(genotype.Drb1, flags[flagValue]) : genotype.Drb1
                };

                diplotypes.Add(diplotype);
            }

            return diplotypes;
        }

        private static LocusInfo<string> GetLocusInfo(LocusInfo<string> locus, bool shouldKeepPositions)
        {
            return shouldKeepPositions ? locus : new LocusInfo<string>() { Position1 = locus.Position2, Position2 = locus.Position1 };
        }

        private static List<Locus> GetHeterozygousLocusTypes(PhenotypeInfo<string> genotype)
        {
            var unusedLocus = new List<Locus>();

            genotype.EachLocus((locus, locusInfo) =>
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