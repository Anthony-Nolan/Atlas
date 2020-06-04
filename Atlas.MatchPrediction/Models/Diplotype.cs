using System;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;

namespace Atlas.MatchPrediction.Models
{
    public class Diplotype : UnorderedPair<Haplotype>
    {
        /// <summary>
        /// Creates a new Diplotype with no inner values for each haplotype.
        /// </summary>
        public Diplotype()
        {
            Item1 = new Haplotype();
            Item2 = new Haplotype();
        }

        /// <summary>
        /// Creates a new Diplotype using the provided LociInfo to set values for Item1 and Item2.
        /// </summary>
        public Diplotype(LociInfo<LocusInfo<string>> source)
        {
            Item1 = new Haplotype
            {
                Hla = new LociInfo<string>
                {
                    A = source.A.Position1,
                    B = source.B.Position1,
                    C = source.C.Position1,
                    Dqb1 = source.Dqb1.Position1,
                    Drb1 = source.Drb1.Position1
                }
            };
            Item2 = new Haplotype
            {
                Hla = new LociInfo<string>
                {
                    A = source.A.Position2,
                    B = source.B.Position2,
                    C = source.C.Position2,
                    Dqb1 = source.Dqb1.Position2,
                    Drb1 = source.Drb1.Position2
                }
            };
        }

        public void SetAtLocus(Locus locus, LocusInfo<string> locusInfo)
        {
            Item1.Hla.SetLocus(locus, locusInfo.Position1);
            Item2.Hla.SetLocus(locus, locusInfo.Position2);
        }

        public void SetAtPosition(Locus locus, LocusPosition position, string value)
        {
            switch (position)
            {
                case LocusPosition.One:
                    Item1.Hla.SetLocus(locus, value);
                    break;
                case LocusPosition.Two:
                    Item1.Hla.SetLocus(locus, value);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(position), position, null);
            }
        }
    }
}
