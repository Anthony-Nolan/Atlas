using System;

namespace Atlas.Functions.Models
{
    public struct OrchestrationStatus
    {
        public string LastCompletedStage { get; set; }
        public TimeSpan? ElapsedTimeOfStage { get; set; }
    } 
}