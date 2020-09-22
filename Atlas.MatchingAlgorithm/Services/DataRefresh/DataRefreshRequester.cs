using System;
using System.Linq;
using System.Threading.Tasks;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.MatchingAlgorithm.Client.Models.DataRefresh;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Data.Persistent.Repositories;
using Atlas.MatchingAlgorithm.Exceptions;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase;
using Atlas.MatchingAlgorithm.Services.DataRefresh.Notifications;

namespace Atlas.MatchingAlgorithm.Services.DataRefresh
{
    public interface IDataRefreshRequester
    {
        /// <summary>
        /// Validates the request to refresh the database and queues it if valid.
        /// </summary>
        /// <returns>Record ID, if request was accepted. Else 400 error thrown with failure details.</returns>
        Task<DataRefreshResponse> RequestDataRefresh(DataRefreshRequest request);
    }

    internal class DataRefreshRequester : IDataRefreshRequester
    {
        private readonly IHlaMetadataDictionary activeVersionHlaMetadataDictionary;
        private readonly IDataRefreshHistoryRepository dataRefreshHistoryRepository;
        private readonly IActiveDatabaseProvider activeDatabaseProvider;
        private readonly IDataRefreshServiceBusClient serviceBusClient;
        private readonly IDataRefreshSupportNotificationSender supportNotificationSender;

        public DataRefreshRequester(
            IHlaMetadataDictionaryFactory hlaMetadataDictionaryFactory,
            IActiveHlaNomenclatureVersionAccessor hlaNomenclatureVersionAccessor,
            IActiveDatabaseProvider activeDatabaseProvider,
            IDataRefreshHistoryRepository dataRefreshHistoryRepository,
            IDataRefreshServiceBusClient serviceBusClient,
            IDataRefreshSupportNotificationSender supportNotificationSender)
        {
            this.activeDatabaseProvider = activeDatabaseProvider;
            this.dataRefreshHistoryRepository = dataRefreshHistoryRepository;
            this.serviceBusClient = serviceBusClient;
            this.supportNotificationSender = supportNotificationSender;

            activeVersionHlaMetadataDictionary = hlaNomenclatureVersionAccessor.DoesActiveHlaNomenclatureVersionExist()
                ? hlaMetadataDictionaryFactory.BuildDictionary(hlaNomenclatureVersionAccessor.GetActiveHlaNomenclatureVersion())
                : null;
        }

        public async Task<DataRefreshResponse> RequestDataRefresh(DataRefreshRequest request)
        {
            if (dataRefreshHistoryRepository.GetIncompleteRefreshJobs().Any())
            {
                throw new InvalidDataRefreshRequestHttpException("Data refresh seems to already be in progress. Data Refresh not started.");
            }

            if (NoNeedToRecreateActiveHlaMetaDictionary() && !request.ForceDataRefresh)
            {
                throw new InvalidDataRefreshRequestHttpException("Active HLA metadata dictionary is already on the " +
                                                             "latest version of the WMDA HLA nomenclature and refresh " +
                                                             "was not run in 'Forced' mode.");
            }

            var recordId = await CreateNewDataRefreshRecord();

            await serviceBusClient.PublishToRequestTopic(new ValidatedDataRefreshRequest { DataRefreshRecordId = recordId });
            await supportNotificationSender.SendInitialisationNotification(recordId);

            return new DataRefreshResponse
            {
                DataRefreshRecordId = recordId
            };
        }

        private bool NoNeedToRecreateActiveHlaMetaDictionary()
        {
            return !(activeVersionHlaMetadataDictionary?.IsActiveVersionDifferentFromLatestVersion() ?? true);
        }

        private async Task<int> CreateNewDataRefreshRecord()
        {
            var dataRefreshRecord = new DataRefreshRecord
            {
                Database = activeDatabaseProvider.GetDormantDatabase().ToString(),
                RefreshRequestedUtc = DateTime.UtcNow,
                HlaNomenclatureVersion = null, //We don't know the version when initially creating the record.
            };

            return await dataRefreshHistoryRepository.Create(dataRefreshRecord);
        }
    }
}