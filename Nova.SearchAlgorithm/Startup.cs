using System.Configuration;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Owin;
using Nova.SearchAlgorithm;
using Nova.SearchAlgorithm.Config;
using Nova.Utils.ApplicationInsights;
using Nova.Utils.WebApi.Owin.Middleware;
using Owin;

[assembly: OwinStartup(typeof(Startup))]
namespace Nova.SearchAlgorithm
{
    [ExcludeFromCodeCoverage]
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.SetUpInstrumentation(ConfigurationManager.AppSettings["insights.instrumentationKey"]);
            var container = app.ConfigureAutofac();

            app.ConfigureHangfire(container);
            
            var logLevel = ConfigurationManager.AppSettings["insights.logLevel"].ToLogLevel();
            app.ConfigureNovaMiddleware("search_algorithm", JsonConfig.GlobalSettings, logLevel);
            app.ConfigureLogging();
            app.ConfigureWebApi(container);
        }
    }
}