using System;
using System.Collections.Generic;
using System.Linq;
using Atlas.Common.GeneticData.PhenotypeInfo;

namespace Atlas.MatchPrediction.Services.GenotypeLikelihood
{
    public interface ISplitGenotype
    {
        public List<PhenotypeInfo<string>> SplitIntoDiplotypes(PhenotypeInfo<string> genotype);
    }

    public class SplitGenotype : ISplitGenotype
    {
        public List<PhenotypeInfo<string>> SplitIntoDiplotypes(PhenotypeInfo<string> genotype)
        {
            return CreateDiplotypes(genotype);
        }

        private static List<PhenotypeInfo<string>> CreateDiplotypes(PhenotypeInfo<string> genotype)
        {
            var diplotypes = new List<PhenotypeInfo<string>>();

            const int n = 5;
            for (var i = 16; i < Math.Pow(2, n); i++)
            {
                var flags = Convert.ToString(i, 2).Select(c => c == '1').ToArray();

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

        private static LocusInfo<string> GetLocusType(LocusInfo<string> locus, bool stay)
        {
            return stay ? locus : new LocusInfo<string>() { Position1 = locus.Position2, Position2 = locus.Position1 };
        }
    }
}