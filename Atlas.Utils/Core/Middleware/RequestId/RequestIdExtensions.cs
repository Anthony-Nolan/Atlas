using System.Linq;
using System.Net.Http;

namespace Atlas.Utils.Core.Middleware.RequestId
{
    public static class RequestIdExtensions
    {
        private const string RequestIdHeader = "X-Request-ID";
        private const string RequestIdSegmentDivider = "|";

        public static void SetRequestId(this HttpRequestMessage message, string requestId)
        {
            message.Headers.Add(RequestIdHeader, requestId);
        }

        public static string GetRequestId(this HttpRequestMessage message)
        {
            var suc = message.Headers.TryGetValues(RequestIdHeader, out var res);
            return suc ? res.Single() : string.Empty;
        }

        public static string AppendRequestIdSegment(this string requestId, string newSegment)
        {
            var requestIdAndDivider =
                string.IsNullOrEmpty(requestId) ? requestId + RequestIdSegmentDivider : string.Empty;
            return requestIdAndDivider + newSegment;
        }
    }
}