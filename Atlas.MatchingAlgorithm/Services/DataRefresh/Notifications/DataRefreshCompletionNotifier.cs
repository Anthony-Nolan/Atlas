using System.Threading.Tasks;
using Atlas.MatchingAlgorithm.Client.Models.DataRefresh;

namespace Atlas.MatchingAlgorithm.Services.DataRefresh.Notifications
{
    public interface IDataRefreshCompletionNotifier
    {
        Task NotifyOfSuccess(int dataRefreshRecordId);
        Task NotifyOfFailure(int dataRefreshRecordId);
    }

    internal class DataRefreshCompletionNotifier : IDataRefreshCompletionNotifier
    {
        private readonly IDataRefreshSupportNotificationSender supportNotificationSender;
        private readonly IDataRefreshServiceBusClient serviceBusClient;

        public DataRefreshCompletionNotifier(
            IDataRefreshSupportNotificationSender supportNotificationSender,
            IDataRefreshServiceBusClient serviceBusClient)
        {
            this.supportNotificationSender = supportNotificationSender;
            this.serviceBusClient = serviceBusClient;
        }

        public async Task NotifyOfSuccess(int dataRefreshRecordId)
        {
            await supportNotificationSender.SendSuccessNotification(dataRefreshRecordId);
            await serviceBusClient.PublishToCompletionTopic(
                new CompletedDataRefresh
                {
                    DataRefreshRecordId = dataRefreshRecordId,
                    WasSuccessful = true
                });
        }

        public async Task NotifyOfFailure(int dataRefreshRecordId)
        {
            await supportNotificationSender.SendFailureAlert(dataRefreshRecordId);
            await serviceBusClient.PublishToCompletionTopic(
                new CompletedDataRefresh
                {
                    DataRefreshRecordId = dataRefreshRecordId,
                    WasSuccessful = false
                });
        }
    }
}
