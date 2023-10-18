using Atlas.Common.ApplicationInsights;
using Atlas.Common.ServiceBus.Exceptions;
using Atlas.Common.Utils;
using Atlas.DonorImport.ExternalInterface.Models;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Exceptions;
using Atlas.MatchingAlgorithm.Services.DonorManagement;
using Microsoft.Azure.WebJobs;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace Atlas.MatchingAlgorithm.Functions.DonorManagement.Functions
{
    public class DonorManagementFunctions
    {
        private const string ErrorMessagePrefix = "Error when running the donor management function. ";

        private readonly IDonorUpdateProcessor donorUpdateProcessor;
        private readonly ILogger logger;

        public DonorManagementFunctions(IDonorUpdateProcessor donorUpdateProcessor, ILogger logger)
        {
            this.donorUpdateProcessor = donorUpdateProcessor;
            this.logger = logger;
        }

        [SuppressMessage(null, SuppressMessage.UnusedParameter, Justification = SuppressMessage.UsedByAzureTrigger)]
        [FunctionName(nameof(ProcessDifferentialDonorUpdatesForMatchingDbA))]
        public async Task ProcessDifferentialDonorUpdatesForMatchingDbA(
            [TimerTrigger("%MessagingServiceBus:DonorManagement:CronSchedule%")]
            TimerInfo myTimer)
        {
            await ProcessDifferentialDonorUpdatesForSpecifiedDb(TransientDatabase.DatabaseA);
        }

        [SuppressMessage(null, SuppressMessage.UnusedParameter, Justification = SuppressMessage.UsedByAzureTrigger)]
        [FunctionName(nameof(ProcessDifferentialDonorUpdatesForMatchingDbB))]
        public async Task ProcessDifferentialDonorUpdatesForMatchingDbB(
            [TimerTrigger("%MessagingServiceBus:DonorManagement:CronSchedule%")]
            TimerInfo myTimer)
        {
            await ProcessDifferentialDonorUpdatesForSpecifiedDb(TransientDatabase.DatabaseB);
        }

        [FunctionName(nameof(ProcessDifferentialDonorUpdatesForMatchingDbADeadLetterQueueListener))]
        public async Task ProcessDifferentialDonorUpdatesForMatchingDbADeadLetterQueueListener(
            [ServiceBusTrigger(
                "%MessagingServiceBus:DonorManagement:Topic%/Subscriptions/%MessagingServiceBus:DonorManagement:SubscriptionForDbA%/$DeadLetterQueue",
                "%MessagingServiceBus:DonorManagement:SubscriptionForDbA%",
                Connection = "MessagingServiceBus:ConnectionString")]
            SearchableDonorUpdate[] searchableDonorUpdates)
        {
            await donorUpdateProcessor.ProcessDeadLetterDifferentialDonorUpdates(searchableDonorUpdates);
        }

        [FunctionName(nameof(ProcessDifferentialDonorUpdatesForMatchingDbBDeadLetterQueueListener))]
        public async Task ProcessDifferentialDonorUpdatesForMatchingDbBDeadLetterQueueListener(
            [ServiceBusTrigger(
                "%MessagingServiceBus:DonorManagement:Topic%/Subscriptions/%MessagingServiceBus:DonorManagement:SubscriptionForDbB%/$DeadLetterQueue",
                "%MessagingServiceBus:DonorManagement:SubscriptionForDbB%",
                Connection = "MessagingServiceBus:ConnectionString")]
            SearchableDonorUpdate[] searchableDonorUpdates)
        {
            await donorUpdateProcessor.ProcessDeadLetterDifferentialDonorUpdates(searchableDonorUpdates);
        }

        private async Task ProcessDifferentialDonorUpdatesForSpecifiedDb(TransientDatabase targetDatabase)
        {
            try
            {
                await donorUpdateProcessor.ProcessDifferentialDonorUpdates(targetDatabase);
            }
            catch (MessageBatchException<SearchableDonorUpdate> ex)
            {
                SendMessageBatchExceptionTrace(ex);
                throw new DonorManagementException(ex);
            }
            catch (Exception ex)
            {
                SendExceptionTrace(ex);
                throw new DonorManagementException(ex);
            }
        }

        private void SendMessageBatchExceptionTrace(MessageBatchException<SearchableDonorUpdate> ex)
        {
            logger.SendTrace(
                ErrorMessagePrefix + ex.Message,
                LogLevel.Error,
                new Dictionary<string, string>
                {
                    {"SequenceNumbers", string.Join(",", ex.SequenceNumbers.Select(seqNo => seqNo))}
                });
        }

        private void SendExceptionTrace(Exception ex)
        {
            logger.SendTrace(ErrorMessagePrefix + ex.Message, LogLevel.Error);
        }
    }
}