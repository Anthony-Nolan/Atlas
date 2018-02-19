using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Formatting;
using System.Web.Http;
using System.Web.Http.ExceptionHandling;
using Autofac;
using Autofac.Integration.WebApi;
using Nova.Utils.Filters;
using Nova.Utils.Pagination;
using Nova.Utils.WebApi.Controllers;
using Nova.Utils.WebApi.ExceptionHandling;
using Nova.Utils.WebApi.Filters;
using Nova.Utils.WebApi.ModelBinders;
using Owin;

namespace Nova.SearchAlgorithm.Config
{
    public static class WebApiConfig
    {
        [ExcludeFromCodeCoverage]
        public static IAppBuilder ConfigureWebApi(this IAppBuilder app, IContainer container)
        {
            var config = CreateConfig(container);
            app.UseAutofacWebApi(config);
            app.UseWebApi(config);
            return app;
        }

        public static HttpConfiguration CreateConfig(IContainer container)
        {
            var config = new HttpConfiguration { DependencyResolver = new AutofacWebApiDependencyResolver(container) };

            config.Formatters.Clear();
            config.Formatters.Add(new JsonMediaTypeFormatter());
            config.Formatters.JsonFormatter.SerializerSettings = JsonConfig.GlobalSettings;

            config.Filters.Add(new ValidModelStateFilter());
            config.Filters.Add(container.Resolve<ApiKeyRequiredAttribute>());
            config.Services.Replace(typeof(IExceptionHandler), new PassthroughExceptionHandler());

            config.ConfigureValidation(container);

            config.ParameterBindingRules.BindFromUriOrEmpty<PaginationData>();
            config.ParameterBindingRules.BindFromUriOrEmpty<FilterBase>();

            config.ConfigureRouting();

            config.EnsureInitialized();
            return config;
        }

        public static void ConfigureRouting(this HttpConfiguration config)
        {
            config.MapHttpAttributeRoutes();
            config.ConfigureSwagger();
            config.AddNovaServiceStatusController();

            // This must be done last as it will capture all requests that are not routed by this point.
            config.AddNovaDefaultController();
        }
    }
}
