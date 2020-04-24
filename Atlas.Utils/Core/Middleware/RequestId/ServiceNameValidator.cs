using System;
using System.Text.RegularExpressions;

namespace Nova.Utils.Middleware.RequestId
{
    public static class ServiceNameValidator
    {
        public static string Validate(string serviceName)
        {
            if (Regex.Match(serviceName, "^[a-zA-Z0-9_]*$").Success)
            {
                return serviceName;
            }
            throw new ArgumentException("Service name " + serviceName + " is not permitted. Only use alphanumeric characters and underscores.");
        }
    }
}