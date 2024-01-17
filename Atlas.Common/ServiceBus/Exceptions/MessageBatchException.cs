using System;
using System.Collections.Generic;
using System.Linq;
using Atlas.Common.Public.Models.ServiceBus;

namespace Atlas.Common.ServiceBus.Exceptions
{
    public class MessageBatchException<T> : Exception
    {
        /// <summary>
        /// Sequence numbers of messages in the batch for which the exception was raised.
        /// </summary>
        public IEnumerable<long> SequenceNumbers { get; set; }

        public MessageBatchException(string processName, IEnumerable<ServiceBusMessage<T>> serviceBusMessages, Exception inner) :
            base(GetErrorMessage(processName), inner)
        {
            SequenceNumbers = serviceBusMessages
                .Select(m => m.SequenceNumber)
                .OrderBy(seqNo => seqNo);
        }

        public static string GetErrorMessage(string processName)
        {
            return $"{processName} : Error when handling the message batch. See the {nameof(SequenceNumbers)} property for the list of affected messages.";
        }
    }
}
