using System.Diagnostics.CodeAnalysis;
using System.Web.Http;
using Nova.Utils.WebApi.Filters;
using Swashbuckle.Application;

namespace Nova.SearchAlgorithm.Config
{
    public static class SwaggerConfig
    {
        [ExcludeFromCodeCoverage]
        public static HttpConfiguration ConfigureSwagger(this HttpConfiguration config)
        {
            config
                .EnableSwagger("docs/{apiVersion}", c =>
                {
                    c.SingleApiVersion("v1", "Search Algorithm API V1");
                    c.IgnoreObsoleteActions();
                    c.IgnoreObsoleteProperties();
                    c.ApiKey("apiKey")
                        .Description("API key authentication")
                        .Name(ApiKeyRequiredAttribute.HeaderKey)
                        .In("header");
                })
                .EnableSwaggerUi(c =>
                {
                    c.EnableApiKeySupport(ApiKeyRequiredAttribute.HeaderKey, "header");
                });

            return config;
        }
    }
}
