using System;
using System.Collections.Generic;

namespace Atlas.Utils.Core.Ordering
{
    /// <summary>
    /// A fluent way of creating comparers. Inspired by the Ordering class from Guava
    /// </summary>
    public static class Order
    {
        public static Order<T> Natural<T>()
        {
            return NaturalOrder<T>.Instance.Value;
        }

        public static Order<T> Explicit<T>(params T[] valuesInOrder)
        {
            return Explicit((IList<T>)valuesInOrder);
        }

        public static Order<T> Explicit<T>(IList<T> valuesInOrder)
        {
            return new ExplicitComparer<T>(valuesInOrder);
        }
    }

    public abstract class Order<T> : Comparer<T>
    {
        public Order<TFrom> OnResultOf<TFrom>(Func<TFrom, T> func)
        {
            return new FuncComparer<TFrom, T>(func, this);
        }
    }
}
