using System;

namespace Atlas.Utils.Core.Ordering
{
    internal class NaturalOrder<T> : Order<T>
    {
        public static Lazy<NaturalOrder<T>> Instance => new Lazy<NaturalOrder<T>>(() => new NaturalOrder<T>());

        public override int Compare(T x, T y)
        {
            return Default.Compare(x, y);
        }
    }
}
