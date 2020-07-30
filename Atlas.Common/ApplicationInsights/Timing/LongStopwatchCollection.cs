using System;
using System.Collections.Generic;

namespace LoggingStopwatch
{
    public class LongStopwatchCollection
    {
        private readonly Dictionary<string, ILongOperationLoggingStopwatch> watches = new Dictionary<string, ILongOperationLoggingStopwatch>();

        private readonly Action<string> defaultLoggingAction;
        private readonly LongLoggingSettings defaultLoggingSettings;

        public LongStopwatchCollection(Action<string> defaultLoggingAction = null, LongLoggingSettings defaultLoggingSettings = null)
        {
            this.defaultLoggingAction = defaultLoggingAction;
            this.defaultLoggingSettings = defaultLoggingSettings;
        }

        public IDisposable InitialiseDisabledStopwatch(string watchKey, string watchDescription = null, Action<string> loggingAction = null, LongLoggingSettings loggingSettings = null)
        {
            var fakeStopwatch = new FastFakeLongOperationLoggingStopwatch();
            watches.Add(watchKey, fakeStopwatch);
            return fakeStopwatch;
        }

        public IDisposable InitialiseStopwatch(string watchKey, string watchDescription = null, Action<string> loggingAction = null, LongLoggingSettings loggingSettings = null)
        {
            var initialisedStopwatch = new LongOperationLoggingStopwatch(
                watchDescription ?? watchKey,
                loggingAction ?? defaultLoggingAction,
                loggingSettings ?? defaultLoggingSettings);
            
            watches.Add(watchKey, initialisedStopwatch);
            return initialisedStopwatch;
        }

        public IDisposable TimeInnerOperation(string watchKey)
        {
            return watches[watchKey].TimeInnerOperation();
        }

        public ILongOperationLoggingStopwatch GetStopwatch(string watchKey)
        {
            return watches[watchKey];
        }
    }
}
