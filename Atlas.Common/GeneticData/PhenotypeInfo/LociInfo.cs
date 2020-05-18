using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

// ReSharper disable MemberCanBeProtected.Global

namespace Atlas.Common.GeneticData.PhenotypeInfo
{
    /// <summary>
    /// Data type to hold one instance of T for each of the supported HLA loci.
    /// </summary>
    /// <typeparam name="T">The type of the information that is required for each locus.</typeparam>
    public class LociInfo<T>
    {
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
        [SuppressMessage("StyleCop.CSharp.DocumentationRules",
            "SA1642:ConstructorSummaryDocumentationMustBeginWithStandardText", Justification = "Disabled.")]
        public LociInfo(T initialValue)
        {
            A = initialValue;
            B = initialValue;
            C = initialValue;
            Dpa1 = initialValue;
            Dpb1 = initialValue;
            Dqa1 = initialValue;
            Dqb1 = initialValue;
            Drb1 = initialValue;
            Drb3 = initialValue;
            Drb4 = initialValue;
            Drb5 = initialValue;
        }

        /// <summary>
        /// Locus A. Used in all search implementations.
        /// </summary>
        public T A { get; set; }

        /// <summary>
        /// Locus B. Used in all search implementations.
        /// </summary>
        public T B { get; set; }

        /// <summary>
        /// Locus C. Used in newer search implementations.
        /// </summary>
        public T C { get; set; }

        /// <summary>
        /// Locus Dpa1. Not used in search.
        /// </summary>
        public T Dpa1 { get; set; }

        /// <summary>
        /// Locus Dpb1. Used in newer search implementations.
        /// </summary>
        public T Dpb1 { get; set; }

        /// <summary>
        /// Locus Dqa1. Not used in search.
        /// </summary>
        public T Dqa1 { get; set; }

        /// <summary>
        /// Locus Dqb1. Used in newer search implementations.
        /// </summary>
        public T Dqb1 { get; set; }

        /// <summary>
        /// Locus Drb1. Used in most search implementations.
        /// </summary>
        public T Drb1 { get; set; }

        /// <summary>
        /// Locus Drb3. Not used in search. Related to Drb4 and Drb5.
        /// A phenotype will contain only two alleles across DRB3/4/5, in any combination.
        /// </summary>
        public T Drb3 { get; set; }

        /// <summary>
        /// Locus Drb4. Not used in search. Related to Drb3 and Drb5.
        /// A phenotype will contain only two alleles across DRB3/4/5, in any combination.
        /// </summary>
        public T Drb4 { get; set; }

        /// <summary>
        /// Locus Drb5. Not used in search. Related to Drb3 and Drb4
        /// A phenotype will contain only two alleles across DRB3/4/5, in any combination.
        /// </summary>
        public T Drb5 { get; set; }

        public static bool operator ==(LociInfo<T> left, LociInfo<T> right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(LociInfo<T> left, LociInfo<T> right)
        {
            return !Equals(left, right);
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
                Dpa1,
                Dpb1,
                Dqa1,
                Dqb1,
                Drb1,
                Drb3,
                Drb4,
                Drb5
            };
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != this.GetType())
            {
                return false;
            }

            return Equals((LociInfo<T>) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = EqualityComparer<T>.Default.GetHashCode(A);
                hashCode = (hashCode * 397) ^ EqualityComparer<T>.Default.GetHashCode(B);
                hashCode = (hashCode * 397) ^ EqualityComparer<T>.Default.GetHashCode(C);
                hashCode = (hashCode * 397) ^ EqualityComparer<T>.Default.GetHashCode(Dpb1);
                hashCode = (hashCode * 397) ^ EqualityComparer<T>.Default.GetHashCode(Dqa1);
                hashCode = (hashCode * 397) ^ EqualityComparer<T>.Default.GetHashCode(Dqb1);
                hashCode = (hashCode * 397) ^ EqualityComparer<T>.Default.GetHashCode(Drb1);
                hashCode = (hashCode * 397) ^ EqualityComparer<T>.Default.GetHashCode(Drb3);
                hashCode = (hashCode * 397) ^ EqualityComparer<T>.Default.GetHashCode(Drb4);
                hashCode = (hashCode * 397) ^ EqualityComparer<T>.Default.GetHashCode(Drb5);
                return hashCode;
            }
        }

        private bool Equals(LociInfo<T> other)
        {
            return EqualityComparer<T>.Default.Equals(A, other.A) &&
                   EqualityComparer<T>.Default.Equals(B, other.B) &&
                   EqualityComparer<T>.Default.Equals(C, other.C) &&
                   EqualityComparer<T>.Default.Equals(Dpa1, other.Dpa1) &&
                   EqualityComparer<T>.Default.Equals(Dpb1, other.Dpb1) &&
                   EqualityComparer<T>.Default.Equals(Dqa1, other.Dqa1) &&
                   EqualityComparer<T>.Default.Equals(Dqb1, other.Dqb1) &&
                   EqualityComparer<T>.Default.Equals(Drb1, other.Drb1) &&
                   EqualityComparer<T>.Default.Equals(Drb3, other.Drb3) &&
                   EqualityComparer<T>.Default.Equals(Drb4, other.Drb4) &&
                   EqualityComparer<T>.Default.Equals(Drb5, other.Drb5);
        }
    }
}