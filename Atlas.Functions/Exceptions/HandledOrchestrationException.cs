using System;

namespace Atlas.Functions.Exceptions
{
    /// <summary>
    /// Used to wrap exceptions that occur in a durable function orchestration instance, and have already been handled - i.e. failure messages sent.
    /// </summary>
    internal class HandledOrchestrationException : Exception
    {
        public HandledOrchestrationException(Exception innerException) : base(innerException?.Message)
        {
            
        }
    }
}