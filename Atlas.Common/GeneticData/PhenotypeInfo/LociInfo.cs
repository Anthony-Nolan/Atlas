using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using EnumStringValues;

// ReSharper disable InconsistentNaming

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
    [DebuggerDisplay("A: {A}, B: {B}, C: {C}, DPB1: {Dpb1}, DQB1: {Dqb1}, DRB1: {Drb1}")]
    public class LociInfo<T> : IEquatable<LociInfo<T>>
    {
        /// <summary>
        /// Locus A. Used in all search implementations.
        /// </summary>
        public T A { get; }

        /// <summary>
        /// Locus B. Used in all search implementations.
        /// </summary>
        public T B { get; }

        /// <summary>
        /// Locus C. Used in newer search implementations.
        /// </summary>
        public T C { get; }

        /// <summary>
        /// Locus Dpb1. Used in newer search implementations.
        /// </summary>
        public T Dpb1 { get; }

        /// <summary>
        /// Locus Dqb1. Used in newer search implementations.
        /// </summary>
        public T Dqb1 { get; }

        /// <summary>
        /// Locus Drb1. Used in most search implementations.
        /// </summary>
        public T Drb1 { get; }

        /// <summary>
        /// Pre-compute hash, so that if this object is used for multiple dictionary lookups, we do not need to calculate the hash multiple times.
        /// This is only safe because this class in immutable, and all properties included in the hash cannot change after construction.
        /// </summary>
        private int? PreComputedHash { get; }

        #region Constructors

        /// <summary>
        /// Creates a new LociInfo with no inner values set.
        /// </summary>
        public LociInfo()
        {
            PreComputedHash = CalculateHashCode();
        }

        /// <summary>
        /// Creates a new LociInfo with all inner values set to the same starting value.
        /// </summary>
        /// <param name="initialValue">The initial value all inner locus values should be given.</param>
        public LociInfo(T initialValue)
        {
            A = initialValue;
            B = initialValue;
            C = initialValue;
            Dpb1 = initialValue;
            Dqb1 = initialValue;
            Drb1 = initialValue;
            PreComputedHash = CalculateHashCode();
        }

        public LociInfo(
            T valueA = default,
            T valueB = default,
            T valueC = default,
            T valueDpb1 = default,
            T valueDqb1 = default,
            T valueDrb1 = default
        )
        {
            A = valueA;
            B = valueB;
            C = valueC;
            Dpb1 = valueDpb1;
            Dqb1 = valueDqb1;
            Drb1 = valueDrb1;
            PreComputedHash = CalculateHashCode();
        }

        public LociInfo(Func<Locus, T> initialValueFactory)
        {
            A = initialValueFactory(Locus.A);
            B = initialValueFactory(Locus.B);
            C = initialValueFactory(Locus.C);
            Dpb1 = initialValueFactory(Locus.Dpb1);
            Dqb1 = initialValueFactory(Locus.Dqb1);
            Drb1 = initialValueFactory(Locus.Drb1);
            PreComputedHash = CalculateHashCode();
        }

        #endregion

        private static ISet<Locus> SupportedLoci => EnumExtensions.EnumerateValues<Locus>().ToHashSet();

        public LociInfo<R> Map<R>(Func<Locus, T, R> mapping)
        {
            return new LociInfo<R>
            (
                mapping(Locus.A, A),
                mapping(Locus.B, B),
                mapping(Locus.C, C),
                mapping(Locus.Dpb1, Dpb1),
                mapping(Locus.Dqb1, Dqb1),
                mapping(Locus.Drb1, Drb1)
            );
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

            return new LociInfo<R>(a.Result, b.Result, c.Result, dpb1.Result, dqb1.Result, drb1.Result);
        }

        public async Task WhenAllLoci(Func<Locus, T, Task> action)
        {
            var a = action(Locus.A, A);
            var b = action(Locus.B, B);
            var c = action(Locus.C, C);
            var dpb1 = action(Locus.Dpb1, Dpb1);
            var dqb1 = action(Locus.Dqb1, Dqb1);
            var drb1 = action(Locus.Drb1, Drb1);

            await Task.WhenAll(a, b, c, dpb1, dqb1, drb1);
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

        public LociInfo<T> SetLoci(T value, params Locus[] loci)
        {
            return loci.Aggregate(this, (result, locus) => result.SetLocus(locus, value));
        }

        public LociInfo<T> SetLocus(Locus locus, T value)
        {
            return locus switch
            {
                Locus.A => new LociInfo<T>(value, B, C, Dpb1, Dqb1, Drb1),
                Locus.B => new LociInfo<T>(A, value, C, Dpb1, Dqb1, Drb1),
                Locus.C => new LociInfo<T>(A, B, value, Dpb1, Dqb1, Drb1),
                Locus.Dpb1 => new LociInfo<T>(A, B, C, value, Dqb1, Drb1),
                Locus.Dqb1 => new LociInfo<T>(A, B, C, Dpb1, value, Drb1),
                Locus.Drb1 => new LociInfo<T>(A, B, C, Dpb1, Dqb1, value),
                _ => throw new ArgumentOutOfRangeException(nameof(locus), locus, null)
            };
        }

        public IEnumerable<T> ToEnumerable()
        {
            return new List<T> {A, B, C, Dpb1, Dqb1, Drb1};
        }

        public PhenotypeInfo<R> ToPhenotypeInfo<R>(Func<Locus, T, R> mapping)
        {
            return new PhenotypeInfo<R>
            (
                new LocusInfo<R>(mapping(Locus.A, A)),
                new LocusInfo<R>(mapping(Locus.B, B)),
                new LocusInfo<R>(mapping(Locus.C, C)),
                new LocusInfo<R>(mapping(Locus.Dpb1, Dpb1)),
                new LocusInfo<R>(mapping(Locus.Dqb1, Dqb1)),
                new LocusInfo<R>(mapping(Locus.Drb1, Drb1))
            );
        }

        public bool EqualsAtLoci(LociInfo<T> other, ISet<Locus> lociToMatchAt)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (this.GetType() != other.GetType())
            {
                return false;
            }

            // Do not commonise with existing equals method as we'll be comparing these a lot and we don't want to add a load of unnecessary lookups.
            return (!lociToMatchAt.Contains(Locus.A) || EqualityComparer<T>.Default.Equals(A, other.A)) &&
                   (!lociToMatchAt.Contains(Locus.B) || EqualityComparer<T>.Default.Equals(B, other.B)) &&
                   (!lociToMatchAt.Contains(Locus.C) || EqualityComparer<T>.Default.Equals(C, other.C)) &&
                   (!lociToMatchAt.Contains(Locus.Dpb1) || EqualityComparer<T>.Default.Equals(Dpb1, other.Dpb1)) &&
                   (!lociToMatchAt.Contains(Locus.Dqb1) || EqualityComparer<T>.Default.Equals(Dqb1, other.Dqb1)) &&
                   (!lociToMatchAt.Contains(Locus.Drb1) || EqualityComparer<T>.Default.Equals(Drb1, other.Drb1));
        }

        /// <returns>
        /// true if the given condition is met at any of the provided loci.
        /// </returns>
        /// <param name="condition">Condition to evaluate per-locus</param>
        /// <param name="loci">If not set, will default to all supported loci.</param>
        public bool AnyAtLoci(Func<Locus, T, bool> condition, ISet<Locus> loci = null)
        {
            loci ??= SupportedLoci;
            return Reduce((locus, value, result) => result || loci.Contains(locus) && condition(locus, value), false);
        }

        /// <returns>
        /// true if the given condition is met at any of the provided loci.
        /// </returns>
        /// <param name="condition">Condition to evaluate per-locus</param>
        /// <param name="loci">If not set, will default to all supported loci.</param>
        public bool AnyAtLoci(Func<T, bool> condition, ISet<Locus> loci = null)
        {
            return AnyAtLoci((_, value) => condition(value), loci);
        }

        /// <returns>
        /// true if the given condition is met at all of the provided loci.
        /// </returns>
        /// <param name="condition">Condition to evaluate per-locus</param>
        /// <param name="loci">If not set, will default to all supported loci.</param>
        public bool AllAtLoci(Func<Locus, T, bool> condition, ISet<Locus> loci = null)
        {
            // x && y == !(!x || !y).
            // This approach is taken over an `Enumerable.All` equivalent, to ensure early return - if one locus fails, we do not need/want to evaluate at other loci.
            // It also allows commonisation of the core logic with the "Any" equivalent
            return !AnyAtLoci((locus, value) => !condition(locus, value), loci);
        }

        /// <returns>
        /// true if the given condition is met at all of the provided loci.
        /// </returns>
        /// <param name="condition">Condition to evaluate per-locus</param>
        /// <param name="loci">If not set, will default to all supported loci.</param>
        public bool AllAtLoci(Func<T, bool> condition, ISet<Locus> loci = null)
        {
            return AllAtLoci((_, value) => condition(value), loci);
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
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (this.GetType() != other.GetType())
            {
                return false;
            }

            return EqualityComparer<T>.Default.Equals(A, other.A) &&
                   EqualityComparer<T>.Default.Equals(B, other.B) &&
                   EqualityComparer<T>.Default.Equals(C, other.C) &&
                   EqualityComparer<T>.Default.Equals(Dpb1, other.Dpb1) &&
                   EqualityComparer<T>.Default.Equals(Dqb1, other.Dqb1) &&
                   EqualityComparer<T>.Default.Equals(Drb1, other.Drb1);
        }

        public override int GetHashCode() => PreComputedHash ?? CalculateHashCode();

        private int CalculateHashCode()
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

    public static class Conversion
    {
        public static PhenotypeInfo<T> ToPhenotypeInfo<T>(this LociInfo<LocusInfo<T>> lociInfo)
        {
            return new PhenotypeInfo<T>(lociInfo);
        }
    }
}