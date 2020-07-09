using System.Collections.Generic;
using System.Linq;

namespace Atlas.Common.Test.SharedTestHelpers.Builders
{
    public static class DictionaryBuilder
    {
        public static Dictionary<TKey, TValue> DictionaryWithCommonValue<TKey, TValue>(TValue defaultValue, params TKey[] keys)
        {
            return keys.ToDictionary(k => k, k => defaultValue);
        }
    }
}