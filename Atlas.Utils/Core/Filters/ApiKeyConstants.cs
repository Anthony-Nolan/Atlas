using System;
using System.Diagnostics.CodeAnalysis;

namespace Nova.Utils.Filters
{
    public class ApiKeyConstants
    {
        [Obsolete("QUERY_STRING_KEY is deprecated, please use QueryStringKey")]
        [SuppressMessage("Microsoft.StyleCop.CSharp.OrderingRules", "SA1310", Justification = "See NOVA-ABCD")]
        public const string QUERY_STRING_KEY = QueryStringKey;

        public const string QueryStringKey = "apiKey";

        [Obsolete("HEADER_KEY is deprecated, please use HeaderKey")]
        [SuppressMessage("Microsoft.StyleCop.CSharp.OrderingRules", "SA1310", Justification = "See NOVA-ABCD")]
        public const string HEADER_KEY = HeaderKey;

        public const string HeaderKey = "X-AnthonyNolan-ApiKey";

        [Obsolete("LEGACY_HEADER_KEY is deprecated, please use LegacyHeaderKey")]
        [SuppressMessage("Microsoft.StyleCop.CSharp.OrderingRules", "SA1310", Justification = "See NOVA-ABCD")]
        public const string LEGACY_HEADER_KEY = LegacyHeaderKey;

        public const string LegacyHeaderKey = "X-Samples-ApiKey";

        public const string FunctionsHeaderKey = "X-Functions-Key";
    }
}