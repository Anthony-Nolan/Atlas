using System.Collections;

namespace Atlas.Utils.Core.Common
{
    public static class EnumerableExtensions
    {
        public static bool IsNullOrEmpty(this IEnumerable source)
        {
            return source == null || source.GetEnumerator().MoveNext() == false;
        }
    }
}
