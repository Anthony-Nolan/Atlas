using System.Diagnostics.CodeAnalysis;
using NLog;
using NLog.Config;
using NLog.Owin.Logging;
using NLog.Targets;
using Owin;

namespace Nova.SearchAlgorithm.Config
{
    public static class LoggingConfig
    {
        [ExcludeFromCodeCoverage]
        public static IAppBuilder ConfigureLogging(this IAppBuilder app)
        {
            var config = new LoggingConfiguration();

            var traceTarget = new TraceTarget();
            config.AddTarget("trace", traceTarget);

            var traceRule = new LoggingRule("*", LogLevel.Debug, traceTarget);
            config.LoggingRules.Add(traceRule);

            LogManager.Configuration = config;

            app.UseNLog();
            return app;
        }
    }
}