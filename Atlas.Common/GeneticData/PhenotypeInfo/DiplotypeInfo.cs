using System;

namespace Atlas.Common.GeneticData.PhenotypeInfo
{
    public class DiplotypeInfo<T> : IEquatable<DiplotypeInfo<T>>
    {
        public LociInfo<T> Haplotype1 { get; set; }
        public LociInfo<T> Haplotype2 { get; set; }
        public LociInfo<T>[] Haplotypes => new[] {Haplotype1, Haplotype2};

        public DiplotypeInfo()
        {
            Initialise();
        }

        public DiplotypeInfo(LociInfo<LocusInfo<T>> source)
        {
            Initialise();
            Haplotype1 = new LociInfo<T>()
            {
                A = source.A.Position1,
                B = source.B.Position1,
                C = source.C.Position1,
                Dqb1 = source.Dqb1.Position1,
                Drb1 = source.Drb1.Position1
            };
            Haplotype2 = new LociInfo<T>()
            {
                A = source.A.Position2,
                B = source.B.Position2,
                C = source.C.Position2,
                Dqb1 = source.Dqb1.Position2,
                Drb1 = source.Drb1.Position2
            };
        }

        private void Initialise()
        {
            Haplotype1 = new LociInfo<T>();
            Haplotype2 = new LociInfo<T>();
        }

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

        public bool Equals(DiplotypeInfo<T> other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Haplotypes.Equals(this.Haplotypes);
        }
    }
}