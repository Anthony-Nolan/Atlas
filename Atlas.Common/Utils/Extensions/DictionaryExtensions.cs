using System.Collections.Generic;

namespace Atlas.Common.Utils.Extensions
{
    public static class DictionaryExtensions
    {
        public static void AddIfNotNullOrEmpty<TKey>(this IDictionary<TKey, string> dictionary, TKey key, string value)
        {
            if (!value.IsNullOrEmpty())
            {
                dictionary.Add(key, value);
            }
        }

        public static void AddIfNonNull<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
        {
            if (value != null)
            {
                dictionary.Add(key, value);
            }
        }
    }
}