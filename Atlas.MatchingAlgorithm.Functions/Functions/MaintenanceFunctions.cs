using System;
using System.Runtime;
using Microsoft.Azure.WebJobs;

namespace Atlas.MatchingAlgorithm.Functions.Functions
{
    public class MaintenanceFunctions
    {
        [FunctionName(nameof(GCCollect))]
        public static void GCCollect([TimerTrigger("%Maintenance:GCCollect:CronSchedule%")] TimerInfo myTimer)
        {
            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect();
        }
    }
}