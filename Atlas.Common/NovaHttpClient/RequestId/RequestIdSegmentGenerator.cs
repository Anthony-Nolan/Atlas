using Atlas.Common.Core.Middleware.RequestId;

namespace Atlas.Common.NovaHttpClient.RequestId
{
    public static class RequestIdSegmentGenerator
    {
        public static string NewSegment(string serviceName)
        {
            return IdGenerator.NewId(ServiceNameValidator.Validate(serviceName));
        }
    }
}
