using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

// ReSharper disable InconsistentNaming - want to use T/R to easily distinguish contained type and target type(s)
// ReSharper disable MemberCanBeInternal

namespace Atlas.Common.GeneticData.PhenotypeInfo
{
    /// <summary>
    /// <see cref="LocusInfo{T}"/> is a single Locus' information - with a T at each position.
    /// A <see cref="LociInfo{T}"/> has a T at each locus.
    /// A <see cref="PhenotypeInfo{T}"/> is a special case of <see cref="LociInfo{T}"/>, where T = LocusInfo.
    /// </summary>
    [DebuggerDisplay("1: {Position1}, 2: {Position2}")]
    // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
    public class LocusInfo<T> : IEquatable<LocusInfo<T>>
    {
        public T Position1 { get; }
        public T Position2 { get; }
        
        /// <summary>
        /// Pre-compute hash, so that if this object is used for multiple dictionary lookups, we do not need to calculate the hash multiple times.
        /// This is only safe because this class in immutable, and all properties included in the hash cannot change after construction.
        /// </summary>
        private int? PreComputedHash { get; }

        #region Constructors

        public LocusInfo()
        {
            PreComputedHash = CalculateHashCode();
        }

        public LocusInfo(T initialValue)
        {
            Position1 = initialValue;
            Position2 = initialValue;
            PreComputedHash = CalculateHashCode();
        }

        public LocusInfo(T position1, T position2)
        {
            Position1 = position1;
            Position2 = position2;
            PreComputedHash = CalculateHashCode();
        }

        public LocusInfo(Func<LocusPosition, T> initialValueFactory)
        {
            Position1 = initialValueFactory(LocusPosition.One);
            Position2 = initialValueFactory(LocusPosition.Two);
            PreComputedHash = CalculateHashCode();
        }

        #endregion

        public IEnumerable<T> ToEnumerable() => new[] {Position1, Position2};
        
        internal LocusInfo<T> ShallowCopy()
        {
            return (LocusInfo<T>) MemberwiseClone();
        }

        public T GetAtPosition(LocusPosition position)
        {
            return position switch
            {
                LocusPosition.One => Position1,
                LocusPosition.Two => Position2,
                _ => throw new ArgumentOutOfRangeException(nameof(position), position, null)
            };
        }

        /// <summary>
        /// Returns a new LocusInfo, with the given value set at the appropriate position.
        /// Does not modify in place, as this class is immutable.
        /// </summary>
        public LocusInfo<T> SetAtPosition(LocusPosition position, T value)
        {
            return position switch
            {
                LocusPosition.One => new LocusInfo<T>(value, Position2),
                LocusPosition.Two => new LocusInfo<T>(Position1, value),
                _ => throw new ArgumentOutOfRangeException(nameof(position), position, null)
            };
        }

        public void EachPosition(Action<LocusPosition, T> action)
        {
            action(LocusPosition.One, Position1);
            action(LocusPosition.Two, Position2);
        }

        #region Functional Methods

        public LocusInfo<R> Map<R>(Func<T, R> mapping)
        {
            return new LocusInfo<R>(mapping(Position1), mapping(Position2));
        }

        public async Task<LocusInfo<R>> MapAsync<R>(Func<T, Task<R>> mapping)
        {
            return new LocusInfo<R>(await mapping(Position1), await mapping(Position2));
        }

        #endregion

        public LocusInfo<T> Swap()
        {
            return new LocusInfo<T>(Position2, Position1);
        }

        public bool Position1And2Null()
        {
            return Position1 == null && Position2 == null;
        }

        public bool Position1And2NotNull()
        {
            return Position1 != null && Position2 != null;
        }

        public bool SinglePositionNull()
        {
            return Position1 == null ^ Position2 == null;
        }

        #region IEquatable<T> implementation (Defers to EqualityComparer of inner type.)
        public static bool operator ==(LocusInfo<T> left, LocusInfo<T> right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(LocusInfo<T> left, LocusInfo<T> right)
        {
            return !Equals(left, right);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as LocusInfo<T>);
        }

        public virtual bool Equals(LocusInfo<T> other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (other.GetType() != GetType())
            {
                return false;
            }

            return EqualityComparer<T>.Default.Equals(Position1, other.Position1) &&
                   EqualityComparer<T>.Default.Equals(Position2, other.Position2);
        }

        public override int GetHashCode() => PreComputedHash ?? CalculateHashCode();

        private int CalculateHashCode()
        {
            unchecked
            {
                var hashCode = EqualityComparer<T>.Default.GetHashCode(Position1);
                hashCode = (hashCode * 397) ^ EqualityComparer<T>.Default.GetHashCode(Position2);
                return hashCode;
            }
        }

        #endregion
    }
}