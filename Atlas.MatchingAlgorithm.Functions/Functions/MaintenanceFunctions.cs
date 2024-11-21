using System;
using System.Runtime;
using Microsoft.Azure.Functions.Worker;

namespace Atlas.MatchingAlgorithm.Functions.Functions
{
    public class MaintenanceFunctions
    {
        [Function(nameof(GCCollect))]
        public static void GCCollect([TimerTrigger("%Maintenance:GCCollect:CronSchedule%")] TimerInfo timer)
        {
            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect();
        }
    }
}