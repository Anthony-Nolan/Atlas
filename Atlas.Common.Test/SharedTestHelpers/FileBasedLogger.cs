using System;
using System.Collections.Generic;
using System.Linq;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Utils.Extensions;
using NLog;
using NLog.Config;
using NLog.Targets;
using Logger = NLog.Logger;
using IAtlasLogger = Atlas.Common.ApplicationInsights.ILogger;
using NLogLevel = NLog.LogLevel;
using AtlasLogLevel = Atlas.Common.ApplicationInsights.LogLevel;

namespace Atlas.Common.Test.SharedTestHelpers
{
    public class FileBasedLogger : IAtlasLogger
    {
        private readonly Logger nLogger;

        public FileBasedLogger()
        {
            var config = new LoggingConfiguration();

            var traceFileInfo = new FileTarget("logfile") { FileName = "${basedir}\\trace.log", ArchiveOldFileOnStartup = true, MaxArchiveDays = 2 };
            var logfileInfo = new FileTarget("logfile") { FileName = "${basedir}\\info.log", ArchiveOldFileOnStartup = true, MaxArchiveDays = 2 };
            var logfileError = new FileTarget("logfile") { FileName = "${basedir}\\error.log", ArchiveOldFileOnStartup = true, MaxArchiveDays = 2 };

            config.AddRule(NLogLevel.Trace, NLogLevel.Trace, traceFileInfo);
            config.AddRule(NLogLevel.Info, NLogLevel.Info, logfileInfo);
            config.AddRule(NLogLevel.Error, NLogLevel.Fatal, logfileError);

            LogManager.Configuration = config;

            nLogger = LogManager.GetCurrentClassLogger();
        }

        public virtual void SendEvent(EventModel eventModel)
        {
            nLogger.Log(GetSeverityLevel(eventModel.Level), eventModel?.ToString());
        }

        public virtual void SendTrace(string message, AtlasLogLevel messageLogLevel, Dictionary<string, string> props)
        {
            var propsString = props?.Select(kvp => $"{kvp.Key}: {kvp.Value}").StringJoin(" | ");
            var messageText = props == null ? message : $"{message}. Properties: {propsString}";
            nLogger.Log(GetSeverityLevel(messageLogLevel), messageText);
        }

        private static NLogLevel GetSeverityLevel(AtlasLogLevel logLevel)
        {
            switch (logLevel)
            {
                case AtlasLogLevel.Verbose:
                    return NLogLevel.Trace;
                case AtlasLogLevel.Info:
                    return NLogLevel.Info;
                case AtlasLogLevel.Warn:
                    return NLogLevel.Warn;
                case AtlasLogLevel.Error:
                    return NLogLevel.Error;
                case AtlasLogLevel.Critical:
                    return NLogLevel.Fatal;
                default:
                    throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null);
            }
        }
    }
}
