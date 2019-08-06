using Nova.SearchAlgorithm.Functions.DonorManagement.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nova.SearchAlgorithm.Functions.DonorManagement.Exceptions
{
    public class MessageProcessorException<T> : Exception
    {
        public MessageProcessorException(IEnumerable<ServiceBusMessage<T>> serviceBusMessages, Exception inner) : 
            base(GetErrorMessage(serviceBusMessages), inner)
        {
        }

        public static string GetErrorMessage(IEnumerable<ServiceBusMessage<T>> messages)
        {
            const string errorMessage = "Error when processing messages with the following sequence numbers: ";

            return errorMessage + string.Join(",", messages.Select(m => m.SequenceNumber));
        }
    }
}
