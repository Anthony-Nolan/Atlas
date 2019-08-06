using Nova.SearchAlgorithm.Functions.DonorManagement.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nova.SearchAlgorithm.Functions.DonorManagement.Exceptions
{
    public class MessageBatchException<T> : Exception
    {
        public MessageBatchException(string processName, IEnumerable<ServiceBusMessage<T>> serviceBusMessages, Exception inner) : 
            base(GetErrorMessage(processName, serviceBusMessages), inner)
        {
        }

        public static string GetErrorMessage(string processName, IEnumerable<ServiceBusMessage<T>> messages)
        {
            const string errorMessage = ": Error when handling messages with the following sequence numbers: ";

            return processName + errorMessage + string.Join(",", messages.Select(m => m.SequenceNumber));
        }
    }
}
