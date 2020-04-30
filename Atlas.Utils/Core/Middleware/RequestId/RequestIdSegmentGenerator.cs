using Atlas.Utils.Core.ApplicationInsights;

namespace Atlas.Utils.Core.Middleware.RequestId
{
    public static class RequestIdSegmentGenerator
    {
        public static string NewSegment(string serviceName)
        {
            return IdGenerator.NewId(ServiceNameValidator.Validate(serviceName));
        }
    }
}
