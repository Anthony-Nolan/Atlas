using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Client.Models.Debug;
using Atlas.Common.ServiceBus;
using Atlas.DonorImport.FileSchema.Models;

namespace Atlas.DonorImport.Services.Debug
{
    public interface IDonorImportResultsPeeker
    {
        Task<IEnumerable<PeekedServiceBusMessage<DonorImportMessage>>> PeekResultsMessages(PeekServiceBusMessagesRequest peekRequest);
    }

    internal class DonorImportResultsPeeker : ServiceBusPeeker<DonorImportMessage>, IDonorImportResultsPeeker
    {
        public DonorImportResultsPeeker(
            IMessageReceiverFactory factory,
            string topicName,
            string subscriptionName) : base(factory, topicName, subscriptionName)
        {
        }

        public async Task<IEnumerable<PeekedServiceBusMessage<DonorImportMessage>>> PeekResultsMessages(PeekServiceBusMessagesRequest peekRequest)
        {
            var messages = await Peek(peekRequest);
            return messages.Select(SetImportInfoPropsIfEmpty);
        }

        /// <summary>
        /// Earlier version of `DonorImportMessage` did not have `SuccessfulImportInfo` and `FailedImportInfo` properties.
        /// Manually set these properties if they are missing using the values held in obsolete props.
        /// TODO: Remove method in Atlas v1.8.0 when obsolete props are deleted.
        /// </summary>
        private static PeekedServiceBusMessage<DonorImportMessage> SetImportInfoPropsIfEmpty(PeekedServiceBusMessage<DonorImportMessage> peekedMessage)
        {
            var messageBody = peekedMessage.DeserializedBody;

            if (messageBody.WasSuccessful && messageBody.SuccessfulImportInfo == null)
            {
                peekedMessage.DeserializedBody.SuccessfulImportInfo = new SuccessfulImportInfo
                {
                    ImportedDonorCount = messageBody.ImportedDonorCount.Value,
                    FailedDonorCount = messageBody.FailedDonorCount.Value,
                    FailedDonorSummary = messageBody.FailedDonorSummary
                };
                return peekedMessage;
            }

            if (!messageBody.WasSuccessful && messageBody.FailedImportInfo == null)
            {
                peekedMessage.DeserializedBody.FailedImportInfo = new FailedImportInfo
                {
                    FileFailureReason = messageBody.FailureReason.Value,
                    FileFailureDescription = messageBody.FailureReasonDescription
                };
                return peekedMessage;
            }

            return peekedMessage;
        }
    }
}