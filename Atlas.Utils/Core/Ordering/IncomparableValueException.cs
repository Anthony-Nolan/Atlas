using System;

namespace Atlas.Utils.Core.Ordering
{
    public class IncomparableValueException : InvalidCastException
    {
        public IncomparableValueException(object value) : base($"Cannot compare value: {value}")
        {
        }
    }
}
