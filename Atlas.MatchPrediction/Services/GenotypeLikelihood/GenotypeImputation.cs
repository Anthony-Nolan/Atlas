using System;
using System.Collections.Generic;
using System.Linq;
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

            const int locusCount = 5;
            for (var i = Math.Pow(2, locusCount - 1); i < Math.Pow(2, locusCount); i++)
            {
                var flags = Convert.ToString((int)i, 2).Select(c => c == '1').ToArray();

                var diplotype = new PhenotypeInfo<string>
                {
                    A = GetLocusType(genotype.A, flags[0]),
                    B = GetLocusType(genotype.B, flags[1]),
                    C = GetLocusType(genotype.C, flags[2]),
                    Dqb1 = GetLocusType(genotype.Dqb1, flags[3]),
                    Drb1 = GetLocusType(genotype.Drb1, flags[4])
                };

                diplotypes.Add(diplotype);
            }

            return diplotypes;
        }

        private static LocusInfo<string> GetLocusType(LocusInfo<string> locus, bool shouldKeepPositions)
        {
            return shouldKeepPositions ? locus : new LocusInfo<string>() { Position1 = locus.Position2, Position2 = locus.Position1 };
        }
    }
}