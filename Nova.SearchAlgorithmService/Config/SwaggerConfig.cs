using System.Diagnostics.CodeAnalysis;
using System.Web.Http;
using Nova.Utils.WebApi.Filters;
using Swashbuckle.Application;

namespace Nova.SearchAlgorithmService.Config
{
    public static class SwaggerConfig
    {
        [ExcludeFromCodeCoverage]
        public static HttpConfiguration ConfigureSwagger(this HttpConfiguration config)
        {
            config
                .EnableSwagger("docs/{apiVersion}", c =>
                {
                    c.SingleApiVersion("v1", "Templates API V1");
                    c.IgnoreObsoleteActions();
                    c.IgnoreObsoleteProperties();
                    c.ApiKey("apiKey")
                        .Description("API key authentication")
                        .Name(ApiKeyRequiredAttribute.HEADER_KEY)
                        .In("header");
                })
                .EnableSwaggerUi(c =>
                {
                    c.EnableApiKeySupport(ApiKeyRequiredAttribute.HEADER_KEY, "header");
                });

            return config;
        }
    }
}
