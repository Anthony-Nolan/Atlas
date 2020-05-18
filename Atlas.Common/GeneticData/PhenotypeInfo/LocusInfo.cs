using System;
using System.Collections.Generic;

namespace Atlas.Common.GeneticData.PhenotypeInfo
{
    public class LocusInfo<T>
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

        public LocusInfo<R> Map<R>(Func<T, R> mapping)
        {
            return new LocusInfo<R>
            {
                Position1 = mapping(Position1),
                Position2 = mapping(Position2)
            };
        }

        public IEnumerable<T> ToEnumerable()
        {
            return new List<T> {Position1, Position2};
        }

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

            return Equals((LocusInfo<T>) obj);
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

        public T GetAtPosition(LocusPosition position)
        {
            switch (position)
            {
                case LocusPosition.Position1:
                    return Position1;
                case LocusPosition.Position2:
                    return Position2;
                default:
                    throw new ArgumentOutOfRangeException(nameof(position), position, null);
            }
        }

        public void SetAtPosition(LocusPosition position, T value)
        {
            switch (position)
            {
                case LocusPosition.Position1:
                    Position1 = value;
                    break;
                case LocusPosition.Position2:
                    Position2 = value;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(position), position, null);
            }
        }

        private bool Equals(LocusInfo<T> other)
        {
            return EqualityComparer<T>.Default.Equals(Position1, other.Position1) &&
                   EqualityComparer<T>.Default.Equals(Position2, other.Position2);
        }
    }
}