using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.Common.Models
{
    /// <summary>
    /// Data type to hold one instance of T for each of the supported HLA loci
    /// </summary>
    /// <typeparam name="T">The type of the information that is required for each locus.</typeparam>
    public class LocusInfo<T>
    {
        public T A { get; set; }
        public T B { get; set; }
        public T C { get; set; }
        public T DPB1 { get; set; }
        public T DQB1 { get; set; }
        public T DRB1 { get; set; }

        // TODO: NOVA-1427: Mapping all positions in parallel using PLINQ may improve performance for long mapping functions
        public LocusInfo<R> Map<R>(Func<Locus, T, R> mapping)
        {
            return new LocusInfo<R>
            {
                A = mapping(Locus.A, A),
                B = mapping(Locus.B, B),
                C = mapping(Locus.C, C),
                DPB1 = mapping(Locus.Dpb1, DPB1),
                DQB1 = mapping(Locus.Dqb1, DQB1),
                DRB1 = mapping(Locus.Drb1, DRB1),
            };
        }
        
        /// <summary>
        /// Maps data at each locus to both positions of a PhenotypeInfo object
        /// </summary>
        // TODO: NOVA-1427: Mapping all positions in parallel using PLINQ may improve performance for long mapping functions
        public PhenotypeInfo<R> ToPhenotypeInfo<R>(Func<Locus, T, R> mapping)
        {
            return new PhenotypeInfo<R>
            {
                A_1 = mapping(Locus.A, A),
                A_2 = mapping(Locus.A, A),
                B_1 = mapping(Locus.B, B),
                B_2 = mapping(Locus.B, B),
                C_1 = mapping(Locus.C, C),
                C_2 = mapping(Locus.C, C),
                DPB1_1 = mapping(Locus.Dpb1, DPB1),
                DPB1_2 = mapping(Locus.Dpb1, DPB1),
                DQB1_1 = mapping(Locus.Dqb1, DQB1),
                DQB1_2 = mapping(Locus.Dqb1, DQB1),
                DRB1_1 = mapping(Locus.Drb1, DRB1),
                DRB1_2 = mapping(Locus.Drb1, DRB1),
            };
        }       
        
        /// <summary>
        /// Maps data at each locus to specific positions of a PhenotypeInfo object
        /// </summary>
        // TODO: NOVA-1427: Mapping all positions in parallel using PLINQ may improve performance for long mapping functions
        public PhenotypeInfo<R> ToPhenotypeInfo<R>(Func<Locus, T, Tuple<R, R>> mapping)
        {
            return new PhenotypeInfo<R>
            {
                A_1 = mapping(Locus.A, A).Item1,
                A_2 = mapping(Locus.A, A).Item2,
                B_1 = mapping(Locus.B, B).Item1,
                B_2 = mapping(Locus.B, B).Item2,
                C_1 = mapping(Locus.C, C).Item1,
                C_2 = mapping(Locus.C, C).Item2,
                DPB1_1 = mapping(Locus.Dpb1, DPB1).Item1,
                DPB1_2 = mapping(Locus.Dpb1, DPB1).Item2,
                DQB1_1 = mapping(Locus.Dqb1, DQB1).Item1,
                DQB1_2 = mapping(Locus.Dqb1, DQB1).Item2,
                DRB1_1 = mapping(Locus.Drb1, DRB1).Item1,
                DRB1_2 = mapping(Locus.Drb1, DRB1).Item2,
            };
        }

        // TODO: NOVA-1427: Running these tasks in parallel could likely improve performance
        public async Task<LocusInfo<R>> MapAsync<R>(Func<Locus, T, Task<R>> mapping)
        {
            return new LocusInfo<R>()
            {
                A = await mapping(Locus.A, A),
                B = await mapping(Locus.B, B),
                C = await mapping(Locus.C, C),
                DPB1 = await mapping(Locus.Dpb1, DPB1),
                DQB1 = await mapping(Locus.Dqb1, DQB1),
                DRB1 = await mapping(Locus.Drb1, DRB1),
            };
        }

        // Aggregates each locus alongside its two values
        public IEnumerable<R> FlatMap<R>(Func<Locus, T, R> mapping)
        {
            return new List<R>
            {
                mapping(Locus.A, A),
                mapping(Locus.B, B),
                mapping(Locus.C, C),
                mapping(Locus.Dpb1, DPB1),
                mapping(Locus.Dqb1, DQB1),
                mapping(Locus.Drb1, DRB1),
            };
        }

        public IEnumerable<T> ToEnumerable()
        {
            return new List<T> {A, B, C, DPB1, DQB1, DRB1};
        }

        public void EachLocus(Action<Locus, T> action)
        {
            action(Locus.A, A);
            action(Locus.B, B);
            action(Locus.C, C);
            action(Locus.Dpb1, DPB1);
            action(Locus.Dqb1, DQB1);
            action(Locus.Drb1, DRB1);
        }

        public T DataAtLocus(Locus locus)
        {
            switch (locus)
            {
                case Locus.A:
                    return A;
                case Locus.B:
                    return B;
                case Locus.C:
                    return C;
                case Locus.Dpb1:
                    return DPB1;
                case Locus.Dqb1:
                    return DQB1;
                case Locus.Drb1:
                    return DRB1;
                default:
                    throw new ArgumentOutOfRangeException(nameof(locus), locus, null);
            }
        }

        public void SetAtLocus(Locus locus, T value)
        {
            switch (locus)
            {
                case Locus.A:
                    A = value;
                    break;
                case Locus.B:
                    B = value;
                    break;
                case Locus.C:
                    C = value;
                    break;
                case Locus.Dpb1:
                    DPB1 = value;
                    break;
                case Locus.Dqb1:
                    DQB1 = value;
                    break;
                case Locus.Drb1:
                    DRB1 = value;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(locus), locus, null);
            }
        }

        #region Equality operators

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((PhenotypeInfo<T>) obj);
        }

        private bool Equals(LocusInfo<T> other)
        {
            return EqualityComparer<T>.Default.Equals(A, other.A) &&
                   EqualityComparer<T>.Default.Equals(B, other.B) &&
                   EqualityComparer<T>.Default.Equals(C, other.C) &&
                   EqualityComparer<T>.Default.Equals(DPB1, other.DPB1) &&
                   EqualityComparer<T>.Default.Equals(DQB1, other.DQB1) &&
                   EqualityComparer<T>.Default.Equals(DRB1, other.DRB1);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = EqualityComparer<T>.Default.GetHashCode(A);
                hashCode = (hashCode * 397) ^ EqualityComparer<T>.Default.GetHashCode(B);
                hashCode = (hashCode * 397) ^ EqualityComparer<T>.Default.GetHashCode(C);
                hashCode = (hashCode * 397) ^ EqualityComparer<T>.Default.GetHashCode(DPB1);
                hashCode = (hashCode * 397) ^ EqualityComparer<T>.Default.GetHashCode(DQB1);
                hashCode = (hashCode * 397) ^ EqualityComparer<T>.Default.GetHashCode(DRB1);
                return hashCode;
            }
        }

        public static bool operator ==(LocusInfo<T> left, LocusInfo<T> right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(LocusInfo<T> left, LocusInfo<T> right)
        {
            return !Equals(left, right);
        }

        #endregion
    }
}