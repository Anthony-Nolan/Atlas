using System;

namespace Nova.Utils.Common
{
    public static class PreconditionExtensions
    {
        public static T AssertArgumentNotNull<T>(this T obj, string paramName) where T : class
        {
            if (obj == null)
            {
                throw new ArgumentNullException(paramName);
            }
            return obj;
        }

        public static T AssertArgument<T>(this T obj, Func<T, bool> predicate, string message)
        {
            if (!predicate(obj))
            {
                throw new ArgumentException(message);
            }
            return obj;
        }

        public static T AssertArgument<T>(this T obj, Func<T, bool> predicate, string message, string paramName)
        {
            if (!predicate(obj))
            {
                throw new ArgumentException(message, paramName);
            }
            return obj;
        }
    }
}
