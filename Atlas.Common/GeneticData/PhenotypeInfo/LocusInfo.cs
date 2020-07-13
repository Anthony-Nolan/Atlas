using System;
using System.Collections.Generic;
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
    public class LocusInfo<T> : IEquatable<LocusInfo<T>>
    {
        public T Position1 { get; set; }
        public T Position2 { get; set; }

        #region Constructors

        public LocusInfo()
        {
        }

        public LocusInfo(T initialValue)
        {
            Position1 = initialValue;
            Position2 = initialValue;
        }

        public LocusInfo(T position1, T position2)
        {
            Position1 = position1;
            Position2 = position2;
        }

        #endregion

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

        public void SetAtPosition(LocusPosition position, T value)
        {
            switch (position)
            {
                case LocusPosition.One:
                    Position1 = value;
                    break;
                case LocusPosition.Two:
                    Position2 = value;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(position), position, null);
            }
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

        public IEnumerable<T> ToEnumerable()
        {
            return new List<T> {Position1, Position2};
        }

        internal bool Position1And2NotNull()
        {
            return Position1 != null && Position2 != null;
        }

        internal bool SinglePositionNull()
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
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            if (other.GetType() != this.GetType()) return false;
            return EqualityComparer<T>.Default.Equals(Position1, other.Position1) &&
                   EqualityComparer<T>.Default.Equals(Position2, other.Position2);
        }

        // TODO: ATLAS-499. This HashCode references mutable properties, which is a BadThing(TM).
        // Make the whole class fully immutable.
        public override int GetHashCode()
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