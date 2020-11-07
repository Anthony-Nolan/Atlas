using System;
using System.Collections.Generic;
using System.Text;

namespace Atlas.MatchingAlgorithm.Data.Helpers
{
    //This isn't ideally located, but since the keys have to be accessed from the .Data project, they can't live in the HlaProcessor, where they might naturally belong.
    public static class DataRefreshTimingKeys
    {
        // ReSharper disable InconsistentNaming
        public const string BatchProgress_TimerKey = "batchProgress";
        public const string HlaExpansion_TimerKey = "hlaExpansion";
        public const string NewPGroupInsertion_Overall_TimerKey = "newPGroupInsertion";
        public const string NewPGroupInsertion_Flattening_TimerKey = "newPGroupInsertion_Flattening";
        public const string NewPGroupInsertion_FindNew_TimerKey = "newPGroupInsertion_FindNew";
        public const string NewHlaNameInsertion_Overall_TimerKey = "newHlaNameInsertion";
        public const string NewHlaNameInsertion_Flattening_TimerKey = "newHlaNameInsertion_Flattening";
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
