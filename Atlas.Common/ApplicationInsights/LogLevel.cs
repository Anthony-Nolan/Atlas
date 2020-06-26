namespace Atlas.Common.ApplicationInsights
{
    public enum LogLevel
    {
        Verbose = 1,
        Info = 2,
        Warn = 3,
        Error = 4,
        Critical = 5
    }

    internal static class LogLevelExtensions
    {
        public static LogLevel ToLogLevel(this string name)
        {
            switch (name.ToUpperInvariant())
            {
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
