using System;
using System.Collections.Generic;

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

        internal LocusInfo<T> ShallowCopy()
        {
            return (LocusInfo<T>) MemberwiseClone();
        }
        
        public LocusInfo<R> Map<R>(Func<T, R> mapping)
        {
            return new LocusInfo<R>(mapping(Position1), mapping(Position2));
        }

        public bool Position1And2NotNull()
        {
            return Position1 != null && Position2 != null;
        }

        public bool SinglePositionNull()
        {
            return Position1 == null ^ Position2 == null;
        }

        public T GetAtPosition(LocusPosition position)
        {
            switch (position)
            {
                case LocusPosition.One:
                    return Position1;
                case LocusPosition.Two:
                    return Position2;
                default:
                    throw new ArgumentOutOfRangeException(nameof(position), position, null);
            }
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