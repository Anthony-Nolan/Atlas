using System.Configuration;
using Nova.Utils.ApplicationInsights;

namespace Nova.SearchAlgorithm.Config
{
    public static class Configuration
    {
        public static readonly string HlaServiceBaseUrl = ConfigurationManager.AppSettings["hlaservice.baseurl"];
        public static readonly string HlaServiceApiKey = ConfigurationManager.AppSettings["hlaservice.apikey"];
    }
}