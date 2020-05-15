using Microsoft.Azure.WebJobs;
using Atlas.MatchingAlgorithm.Models;
using Atlas.MatchingAlgorithm.Exceptions;
using Atlas.MatchingAlgorithm.Services.DonorManagement;
using Atlas.Common.ServiceBus.Exceptions;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Utils;

namespace Atlas.MatchingAlgorithm.Functions.DonorManagement.Functions
{
    public class DonorManagement
    {
        const string ErrorMessagePrefix = "Error when running the donor management function. ";

        private readonly IDonorUpdateProcessor donorUpdateProcessor;
        private readonly ILogger logger;

        public DonorManagement(IDonorUpdateProcessor donorUpdateProcessor, ILogger logger)
        {
            this.donorUpdateProcessor = donorUpdateProcessor;
            this.logger = logger;
        }

        [SuppressMessage(null, SuppressMessage.UnusedParameter, Justification = SuppressMessage.UsedByAzureTrigger)]
        [FunctionName("ManageDonorByAvailability")]
        public async Task Run([TimerTrigger("%MessagingServiceBus:DonorManagement:CronSchedule%")] TimerInfo myTimer)
        {
            try
            {
                await donorUpdateProcessor.ProcessDonorUpdates();
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