using System;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;

namespace Atlas.MatchPrediction.Models
{
    internal class Diplotype : UnorderedPair<Haplotype>
    {
        /// <summary>
        /// Creates a new Diplotype with no inner values for each haplotype.
        /// </summary>
        public Diplotype() : base(new Haplotype(), new Haplotype())
        {
        }

        public Diplotype(Haplotype haplotype1, Haplotype haplotype2) : base(haplotype1, haplotype2)
        {
            
        }

        /// <summary>
        /// Creates a new Diplotype using the provided LociInfo to set values for Item1 and Item2.
        /// </summary>
        public Diplotype(LociInfo<LocusInfo<string>> source) : base(
            new Haplotype
            {
                Hla = new LociInfo<string>
                (
                    valueA: source.A.Position1,
                    valueB: source.B.Position1,
                    valueC: source.C.Position1,
                    valueDqb1: source.Dqb1.Position1,
                    valueDrb1: source.Drb1.Position1
                )
            },
            new Haplotype
            {
                Hla = new LociInfo<string>
                (
                    valueA: source.A.Position2,
                    valueB: source.B.Position2,
                    valueC: source.C.Position2,
                    valueDqb1: source.Dqb1.Position2,
                    valueDrb1: source.Drb1.Position2
                )
            })
        {
        }

        public void SetAtLocus(Locus locus, LocusInfo<string> locusInfo)
        {
            Item1.Hla = Item1.Hla.SetLocus(locus, locusInfo.Position1);
            Item2.Hla = Item2.Hla.SetLocus(locus, locusInfo.Position2);
        }

        public void SetAtPosition(Locus locus, LocusPosition position, string value)
        {
            Item1.Hla = position switch
            {
                LocusPosition.One => Item1.Hla.SetLocus(locus, value),
                LocusPosition.Two => Item1.Hla.SetLocus(locus, value),
                _ => throw new ArgumentOutOfRangeException(nameof(position), position, null)
            };
        }
    }
}