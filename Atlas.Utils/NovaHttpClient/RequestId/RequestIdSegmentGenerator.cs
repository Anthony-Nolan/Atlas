using Atlas.Utils.Core.ApplicationInsights;
using Atlas.Utils.Core.Middleware.RequestId;

namespace Atlas.Utils.NovaHttpClient.RequestId
{
    public static class RequestIdSegmentGenerator
    {
        public static string NewSegment(string serviceName)
        {
            return IdGenerator.NewId(ServiceNameValidator.Validate(serviceName));
        }
    }
}
