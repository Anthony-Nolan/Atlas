using System;

namespace Nova.Utils.Ordering
{
    public class IncomparableValueException : InvalidCastException
    {
        public IncomparableValueException(object value) : base($"Cannot compare value: {value}")
        {
        }
    }
}
