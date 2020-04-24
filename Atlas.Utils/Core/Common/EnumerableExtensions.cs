using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;

namespace Atlas.Utils.Core.Common
{
    public static class EnumerableExtensions
    {
        private static readonly ConcurrentDictionary<Type, MethodInfo> GenericCastMethods =
            new ConcurrentDictionary<Type, MethodInfo>();

        public static IEnumerable Cast(this IEnumerable source, Type innerType)
        {
            var genericMethod = GenericCastMethods.GetOrAdd(innerType, t =>
            {
                var methodInfo = typeof(Enumerable).GetMethod(nameof(Enumerable.Cast));
                return methodInfo.MakeGenericMethod(innerType);
            });
            return genericMethod.Invoke(null, new object[] { source }) as IEnumerable;
        }
    }
}
