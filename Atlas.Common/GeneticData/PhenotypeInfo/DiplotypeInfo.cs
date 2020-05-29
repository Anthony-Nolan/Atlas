using System;

namespace Atlas.Common.GeneticData.PhenotypeInfo
{
    /// <summary>
    /// Data type to hold an instance of T as a pair of haplotypes for each of the supported HLA loci.
    /// 
    /// <see cref="LociInfo{T}"/> has a T at each locus.
    /// </summary>
    /// <typeparam name="T">The type of the information that is required for each loci.</typeparam>
    public class DiplotypeInfo<T>
    {
        public LociInfo<T> Haplotype1 { get; set; }
        public LociInfo<T> Haplotype2 { get; set; }

        public DiplotypeInfo()
        {
            Haplotype1 = new LociInfo<T>();
            Haplotype2 = new LociInfo<T>();
        }

        public DiplotypeInfo(LociInfo<LocusInfo<T>> source)
        {
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

        public void SetAtLocus(Locus locus, LocusInfo<T> locusInfo)
        {
            Haplotype1.SetLocus(locus, locusInfo.Position1);
            Haplotype2.SetLocus(locus, locusInfo.Position2);
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

        #region Equality

        /// <summary>
        /// Diplotypes are considered the same regardless of the order of the represented haplotypes.
        /// </summary>
        public override bool Equals(object other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (other.GetType() != this.GetType())
            {
                return false;
            }

            return Equals((DiplotypeInfo<T>)other);
        }

        protected bool Equals(DiplotypeInfo<T> other)
        {
            return Haplotype1.Equals(other.Haplotype2) && Haplotype2.Equals(other.Haplotype1)
                   || Haplotype1.Equals(other.Haplotype1) && Haplotype2.Equals(other.Haplotype2);
        }

        public override int GetHashCode()
        {
            return Haplotype1.GetHashCode() ^ Haplotype2.GetHashCode();
        }

        #endregion
    }
}