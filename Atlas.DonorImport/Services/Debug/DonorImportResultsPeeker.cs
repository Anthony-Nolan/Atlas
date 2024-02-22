using Atlas.Common.Debugging;
using Atlas.Common.ServiceBus;
using Atlas.Debug.Client.Models.ServiceBus;
using Atlas.DonorImport.FileSchema.Models;
using System.Linq;
using System.Threading.Tasks;

namespace Atlas.DonorImport.Services.Debug
{
    public interface IDonorImportResultsPeeker
    {
        Task<PeekServiceBusMessagesResponse<DonorImportMessage>> PeekResultsMessages(PeekServiceBusMessagesRequest peekRequest);
    }

    internal class DonorImportResultsPeeker : ServiceBusPeeker<DonorImportMessage>, IDonorImportResultsPeeker
    {
        public DonorImportResultsPeeker(
            IMessageReceiverFactory factory,
            string connectionString,
            string topicName,
            string subscriptionName) : base(factory, connectionString, topicName, subscriptionName)
        {
        }

        public async Task<PeekServiceBusMessagesResponse<DonorImportMessage>> PeekResultsMessages(PeekServiceBusMessagesRequest peekRequest)
        {
            var response = await Peek(peekRequest);
            var messages = response.PeekedMessages.Select(SetImportInfoPropsIfEmpty);
            
            return new PeekServiceBusMessagesResponse<DonorImportMessage>
            {
                PeekedMessages = messages,
                MessageCount = response.MessageCount
            };
        }

        /// <summary>
        /// Earlier version of `DonorImportMessage` did not have `SuccessfulImportInfo` and `FailedImportInfo` properties.
        /// Manually set these properties if they are missing using the values held in obsolete props.
        /// TODO: Remove method in Atlas v1.8.0 when obsolete props are deleted.
        /// </summary>
        private static DonorImportMessage SetImportInfoPropsIfEmpty(DonorImportMessage peekedMessage)
        {
            if (peekedMessage.WasSuccessful && peekedMessage.SuccessfulImportInfo == null)
            {
                peekedMessage.SuccessfulImportInfo = new SuccessfulImportInfo
                {
                    ImportedDonorCount = peekedMessage.ImportedDonorCount.Value,
                    FailedDonorCount = peekedMessage.FailedDonorCount.Value,
                    FailedDonorSummary = peekedMessage.FailedDonorSummary
                };
                return peekedMessage;
            }

            if (!peekedMessage.WasSuccessful && peekedMessage.FailedImportInfo == null)
            {
                peekedMessage.FailedImportInfo = new FailedImportInfo
                {
                    FileFailureReason = peekedMessage.FailureReason.Value,
                    FileFailureDescription = peekedMessage.FailureReasonDescription
                };
                return peekedMessage;
            }

            return peekedMessage;
        }
    }
}