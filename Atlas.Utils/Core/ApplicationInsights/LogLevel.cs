namespace Nova.Utils.ApplicationInsights
{
    public enum LogLevel
    {
        Trace = 0,
        Verbose = 1,
        Info = 2,
        Warn = 3,
        Error = 4,
        Critical = 5
    }

    public static class LogLevelExtensions
    {
        public static LogLevel ToLogLevel(this string name)
        {
            switch (name.ToUpperInvariant())
            {
                case "TRACE":
                    return LogLevel.Trace;
                case "VERBOSE":
                    return LogLevel.Verbose;
                case "INFO":
                case "INFORMATION":
                    return LogLevel.Info;
                case "WARN":
                case "WARNING":
                    return LogLevel.Warn;
                default:
                    return LogLevel.Error;
            }
        }
    }
}
