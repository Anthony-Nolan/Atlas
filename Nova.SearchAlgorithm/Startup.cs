using System.Configuration;
using System.Diagnostics.CodeAnalysis;
using System.Web.Hosting;
using Autofac;
using Microsoft.Owin;
using Nova.SearchAlgorithm.Config;
using Nova.SearchAlgorithm;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using Nova.SearchAlgorithm.Services;
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
            
            HostingEnvironment.QueueBackgroundWorkItem(clt => container.Resolve<IAntigenCachingService>().GenerateAntigenCache());
            HostingEnvironment.QueueBackgroundWorkItem(clt => container.Resolve<IMatchingDictionaryRepository>().LoadMatchingDictionaryIntoMemory());
            
            var logLevel = ConfigurationManager.AppSettings["insights.logLevel"].ToLogLevel();
            app.ConfigureNovaMiddleware("search_algorithm", JsonConfig.GlobalSettings, logLevel);
            app.ConfigureLogging();
            app.ConfigureWebApi(container);
        }
    }
}