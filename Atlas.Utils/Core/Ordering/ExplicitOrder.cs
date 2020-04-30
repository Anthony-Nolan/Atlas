using System.Collections.Generic;

namespace Atlas.Utils.Core.Ordering
{
    internal class ExplicitComparer<T> : Order<T>
    {
        private readonly IDictionary<T, int> ranks;

        public ExplicitComparer(IList<T> valuesInOrder)
        {
            ranks = new Dictionary<T, int>(valuesInOrder.Count);
            for (var i = 0; i < valuesInOrder.Count; i++)
            {
                ranks[valuesInOrder[i]] = i;
            }
        }

        public override int Compare(T x, T y)
        {
            return Rank(x).CompareTo(Rank(y));
        }

        private int Rank(T value)
        {
            try
            {
                return ranks[value];
            }
            catch (KeyNotFoundException)
            {
                throw new IncomparableValueException(value);
            }
        }
    }
}