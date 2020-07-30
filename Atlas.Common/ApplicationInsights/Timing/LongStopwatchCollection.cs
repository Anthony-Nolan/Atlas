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

    //This isn't ideal, but since the keys have to be accessed from a wide range of projects, they ultimately have to live in Common :(
    public static class StopwatchKeys {
        public static class HlaProcessor
        {
            // ReSharper disable InconsistentNaming
            public const string BatchProgress_TimerKey = "batchProgress";
            public const string HlaExpansion_TimerKey = "hlaExpansion";
            public const string NewPGroupInsertion_Overall_TimerKey = "newPGroupInsertion";
            public const string NewPGroupInsertion_Flattening_TimerKey = "newPGroupInsertion_Flattening";
            public const string NewPGroupInsertion_FindNew_TimerKey = "newPGroupInsertion_FindNew";
            public const string HlaUpsert_Overall_TimerKey = "upsert";
            public const string HlaUpsert_BulkInsertSetup_Overall_TimerKey = "pGroupInsertSetup";
            public const string HlaUpsert_BulkInsertSetup_BuildDataTable_Overall_TimerKey = "pGroupInsertSetup_BuildDataTable";
            public const string HlaUpsert_BulkInsertSetup_BuildDataTable_CreateDtObject_TimerKey = "pGroupInsertSetup_CreateDataTableObject";
            public const string HlaUpsert_BulkInsertSetup_BuildDataTable_OutsideForeach_TimerKey = "pGroupInsertSetup_CreateDataTable_OutsideForeach";
            public const string HlaUpsert_BulkInsertSetup_BuildDataTable_InsideForeach_TimerKey = "pGroupInsertSetup_CreateDataTable_InsideForeach";
            public const string HlaUpsert_BulkInsertSetup_BuildDataTable_FetchPGroupId_TimerKey = "pGroupInsertSetup_FetchPGroupId";
            public const string HlaUpsert_BulkInsertSetup_BuildDataTable_AddRowToDt_TimerKey = "pGroupInsertSetup_AddRowsToDataTable";
            public const string HlaUpsert_BulkInsertSetup_DeleteExistingRecords_TimerKey = "pGroupInsertSetup_DeleteExistingRecords";
            public const string HlaUpsert_BlockingWait_TimerKey = "pGroupLinearWait";
            public const string HlaUpsert_DtWriteExecution_TimerKey = "pGroupDbInsert";
            // ReSharper restore InconsistentNaming
        }
    }
}
