using Nova.Utils.ApplicationInsights;

namespace Nova.Utils.Middleware.RequestId
{
    public static class RequestIdSegmentGenerator
    {
        public static string NewSegment(string serviceName)
        {
            return IdGenerator.NewId(ServiceNameValidator.Validate(serviceName));
        }
    }
}
