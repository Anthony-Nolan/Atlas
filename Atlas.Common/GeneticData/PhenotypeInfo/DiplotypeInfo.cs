using System;

namespace Atlas.Common.GeneticData.PhenotypeInfo
{
    public class DiplotypeInfo<T>
    {
        public LociInfo<T> Haplotype1 { get; set; }
        public LociInfo<T> Haplotype2 { get; set; }

        public LociInfo<T>[] Haplotypes => new[] {Haplotype1, Haplotype2};

        public void SetAtPosition(Locus locus, LocusPosition position, T value)
        {
            switch (position)
            {
                case LocusPosition.One:
                    Haplotype1.SetLocus(locus, value);
                    break;
                case LocusPosition.Two:
                    Haplotype2.SetLocus(locus, value);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(position), position, null);
            }
        }
    }
}