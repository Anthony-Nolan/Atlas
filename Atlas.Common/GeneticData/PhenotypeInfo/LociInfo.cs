using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// ReSharper disable MemberCanBeProtected.Global

namespace Atlas.Common.GeneticData.PhenotypeInfo
{
    /// <summary>
    /// Data type to hold one instance of T for each of the supported HLA loci.
    ///
    /// <see cref="LocusInfo{T}"/> is a single Locus' information - with a T at each position.
    /// A <see cref="LociInfo{T}"/> has a T at each locus.
    /// A <see cref="PhenotypeInfo{T}"/> is a special case of <see cref="LociInfo{T}"/>, where T = LocusInfo.
    /// </summary>
    /// <typeparam name="T">The type of the information that is required for each locus.</typeparam>
    public class LociInfo<T> : IEquatable<LociInfo<T>>
    {
        // ReSharper disable InconsistentNaming - recommended name clashes with property!
        protected T a;
        protected T b;
        protected T c;
        protected T dpb1;
        protected T dqb1;
        protected T drb1;
        // ReSharper restore InconsistentNaming

        /// <summary>
        /// Creates a new LociInfo with no inner values set.
        /// </summary>
        public LociInfo()
        {
        }

        /// <summary>
        /// Creates a new LociInfo with all inner values set to the same starting value.
        /// </summary>
        /// <param name="initialValue">The initial value all inner locus values should be given.</param>
        public LociInfo(T initialValue)
        {
            a = initialValue;
            b = initialValue;
            c = initialValue;
            dpb1 = initialValue;
            dqb1 = initialValue;
            drb1 = initialValue;
        }

        /// <summary>
        /// Locus A. Used in all search implementations.
        /// </summary>
        public virtual T A
        {
            get => a;
            set => a = value;
        }

        /// <summary>
        /// Locus B. Used in all search implementations.
        /// </summary>
        public virtual T B
        {
            get => b;
            set => b = value;
        }

        /// <summary>
        /// Locus C. Used in newer search implementations.
        /// </summary>
        public virtual T C
        {
            get => c;
            set => c = value;
        }

        /// <summary>
        /// Locus Dpb1. Used in newer search implementations.
        /// </summary>
        public virtual T Dpb1
        {
            get => dpb1;
            set => dpb1 = value;
        }

        /// <summary>
        /// Locus Dqb1. Used in newer search implementations.
        /// </summary>
        public virtual T Dqb1
        {
            get => dqb1;
            set => dqb1 = value;
        }

        /// <summary>
        /// Locus Drb1. Used in most search implementations.
        /// </summary>
        public virtual T Drb1
        {
            get => drb1;
            set => drb1 = value;
        }

        public LociInfo<R> Map<R>(Func<Locus, T, R> mapping)
        {
            return new LociInfo<R>
            {
                A = mapping(Locus.A, A),
                B = mapping(Locus.B, B),
                C = mapping(Locus.C, C),
                Dpb1 = mapping(Locus.Dpb1, Dpb1),
                Dqb1 = mapping(Locus.Dqb1, Dqb1),
                Drb1 = mapping(Locus.Drb1, Drb1),
            };
        }

        public LociInfo<R> Map<R>(Func<T, R> mapping)
        {
            return Map((locusType, locusInfo) => mapping(locusInfo));
        }

        public async Task<LociInfo<R>> MapAsync<R>(Func<Locus, T, Task<R>> mapping)
        {
            var a = mapping(Locus.A, A);
            var b = mapping(Locus.B, B);
            var c = mapping(Locus.C, C);
            var dpb1 = mapping(Locus.Dpb1, Dpb1);
            var dqb1 = mapping(Locus.Dqb1, Dqb1);
            var drb1 = mapping(Locus.Drb1, Drb1);

            await Task.WhenAll(a, b, c, dpb1, dqb1, drb1);

            return new LociInfo<R>
            {
                A = a.Result,
                B = b.Result,
                C = c.Result,
                Dpb1 = dpb1.Result,
                Dqb1 = dqb1.Result,
                Drb1 = drb1.Result,
            };
        }

        public R Reduce<R>(Func<Locus, T, R, R> reducer, R initialValue = default)
        {
            var result = initialValue;
            result = reducer(Locus.A, A, result);
            result = reducer(Locus.B, B, result);
            result = reducer(Locus.C, C, result);
            result = reducer(Locus.Dpb1, Dpb1, result);
            result = reducer(Locus.Dqb1, Dqb1, result);
            result = reducer(Locus.Drb1, Drb1, result);
            return result;
        }

        public T GetLocus(Locus locus)
        {
            return locus switch
            {
                Locus.A => A,
                Locus.B => B,
                Locus.C => C,
                Locus.Dpb1 => Dpb1,
                Locus.Dqb1 => Dqb1,
                Locus.Drb1 => Drb1,
                _ => throw new ArgumentOutOfRangeException(nameof(locus), locus, null)
            };
        }

        public void SetLocus(Locus locus, T value)
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
                    Dpb1 = value;
                    break;
                case Locus.Dqb1:
                    Dqb1 = value;
                    break;
                case Locus.Drb1:
                    Drb1 = value;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(locus), locus, null);
            }
        }

        public IEnumerable<T> ToEnumerable()
        {
            return new List<T>
            {
                A,
                B,
                C,
                Dpb1,
                Dqb1,
                Drb1,
            };
        }

        public PhenotypeInfo<R> ToPhenotypeInfo<R>(Func<Locus, T, R> mapping)
        {
            return new PhenotypeInfo<R>
            {
                A =
                {
                    Position1 = mapping(Locus.A, A),
                    Position2 = mapping(Locus.A, A),
                },
                B =
                {
                    Position1 = mapping(Locus.B, B),
                    Position2 = mapping(Locus.B, B),
                },
                C =
                {
                    Position1 = mapping(Locus.C, C),
                    Position2 = mapping(Locus.C, C),
                },
                Dpb1 =
                {
                    Position1 = mapping(Locus.Dpb1, Dpb1),
                    Position2 = mapping(Locus.Dpb1, Dpb1),
                },
                Dqb1 =
                {
                    Position1 = mapping(Locus.Dqb1, Dqb1),
                    Position2 = mapping(Locus.Dqb1, Dqb1),
                },
                Drb1 =
                {
                    Position1 = mapping(Locus.Drb1, Drb1),
                    Position2 = mapping(Locus.Drb1, Drb1),
                }
            };
        }

        #region IEquatable<T> implementation (Defers to EqualityComparer of inner type.)
        public static bool operator ==(LociInfo<T> left, LociInfo<T> right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(LociInfo<T> left, LociInfo<T> right)
        {
            return !Equals(left, right);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as LociInfo<T>);
        }

        public virtual bool Equals(LociInfo<T> other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            if (this.GetType() != other.GetType()) return false;

            return EqualityComparer<T>.Default.Equals(A, other.A) &&
                   EqualityComparer<T>.Default.Equals(B, other.B) &&
                   EqualityComparer<T>.Default.Equals(C, other.C) &&
                   EqualityComparer<T>.Default.Equals(Dpb1, other.Dpb1) &&
                   EqualityComparer<T>.Default.Equals(Dqb1, other.Dqb1) &&
                   EqualityComparer<T>.Default.Equals(Drb1, other.Drb1);
        }

        public virtual bool Equals(LociInfo<T> other, ISet<Locus> lociToMatchAt)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            if (this.GetType() != other.GetType()) return false;

            return (!lociToMatchAt.Contains(Locus.A) || EqualityComparer<T>.Default.Equals(A, other.A)) &&
                   (!lociToMatchAt.Contains(Locus.B) || EqualityComparer<T>.Default.Equals(B, other.B)) &&
                   (!lociToMatchAt.Contains(Locus.C) || EqualityComparer<T>.Default.Equals(C, other.C)) &&
                   (!lociToMatchAt.Contains(Locus.Dpb1) || EqualityComparer<T>.Default.Equals(Dpb1, other.Dpb1)) &&
                   (!lociToMatchAt.Contains(Locus.Dqb1) || EqualityComparer<T>.Default.Equals(Dqb1, other.Dqb1)) &&
                   (!lociToMatchAt.Contains(Locus.Drb1) || EqualityComparer<T>.Default.Equals(Drb1, other.Drb1));
        }

        // TODO: ATLAS-499. This HashCode references mutable properties, which is a BadThing(TM).
        // Make the whole class fully immutable.
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = EqualityComparer<T>.Default.GetHashCode(A);
                hashCode = (hashCode * 397) ^ EqualityComparer<T>.Default.GetHashCode(B);
                hashCode = (hashCode * 397) ^ EqualityComparer<T>.Default.GetHashCode(C);
                hashCode = (hashCode * 397) ^ EqualityComparer<T>.Default.GetHashCode(Dpb1);
                hashCode = (hashCode * 397) ^ EqualityComparer<T>.Default.GetHashCode(Dqb1);
                hashCode = (hashCode * 397) ^ EqualityComparer<T>.Default.GetHashCode(Drb1);
                return hashCode;
            }
        }
        #endregion
    }
}