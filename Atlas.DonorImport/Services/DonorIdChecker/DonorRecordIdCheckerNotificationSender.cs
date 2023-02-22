using System;
using System.Text;
using Atlas.Common.ServiceBus;
using Atlas.DonorImport.ExternalInterface.Settings.ServiceBus;
using Microsoft.Azure.ServiceBus;
using System.Threading.Tasks;
using System.Transactions;
using Atlas.Common.Utils;
using Atlas.DonorImport.ExternalInterface.Settings;
using Newtonsoft.Json;
using Atlas.DonorImport.FileSchema.Models.DonorIdChecker;
using Atlas.Common.Notifications;

namespace Atlas.DonorImport.Services.DonorIdChecker
{
    public interface IDonorRecordIdCheckerNotificationSender
    {
        Task SendNotification(string summary, string description);
    }

    internal class DonorRecordIdCheckerNotificationSender : IDonorRecordIdCheckerNotificationSender
    {
        private readonly ITopicClient topicClient;

        public DonorRecordIdCheckerNotificationSender(NotificationsServiceBusSettings notificationServiceBusSettings, ITopicClientFactory topicClientFactory)
        {
            topicClient = topicClientFactory.BuildTopicClient(notificationServiceBusSettings.ConnectionString,
                notificationServiceBusSettings.DonorIdCheckerResultsTopic);
        }

        public async Task SendNotification(string summary, string description)
        {
            try
            {
                var messageJson = JsonConvert.SerializeObject(new DonorIdCheckerNotification(summary, description));
                var message = new Message(Encoding.UTF8.GetBytes(messageJson));
                using (new AsyncTransactionScope(TransactionScopeOption.Suppress))
                {
                    await topicClient.SendAsync(message);
                }
            }
            catch
            {
            }
        }
    }
}
