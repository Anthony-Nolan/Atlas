using System;
using System.Collections.Generic;
using Nova.Utils.Common;

namespace Nova.Utils.Ordering
{
    internal class FuncComparer<TFrom, TTo> : Order<TFrom>
    {
        private readonly Func<TFrom, TTo> func;
        private readonly IComparer<TTo> inner;

        public FuncComparer(Func<TFrom, TTo> func, IComparer<TTo> inner)
        {
            this.func = func.AssertArgumentNotNull(nameof(func));
            this.inner = inner.AssertArgumentNotNull(nameof(inner));
        }

        public override int Compare(TFrom x, TFrom y)
        {
            return inner.Compare(func(x), func(y));
        }
    }
}