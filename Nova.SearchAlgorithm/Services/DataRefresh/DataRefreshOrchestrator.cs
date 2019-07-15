using System.Linq;
using System.Threading.Tasks;
using Nova.SearchAlgorithm.Data.Persistent.Repositories;
using Nova.SearchAlgorithm.Services.ConfigurationProviders;

namespace Nova.SearchAlgorithm.Services.DataRefresh
{
    public interface IDataRefreshOrchestrator
    {
        Task RefreshDataIfNecessary();
    }

    public class DataRefreshOrchestrator : IDataRefreshOrchestrator
    {
        private readonly IWmdaHlaVersionProvider wmdaHlaVersionProvider;
        private readonly IDataRefreshService dataRefreshService;
        private readonly IDataRefreshHistoryRepository dataRefreshHistoryRepository;

        public DataRefreshOrchestrator(
            IWmdaHlaVersionProvider wmdaHlaVersionProvider,
            IDataRefreshService dataRefreshService,
            IDataRefreshHistoryRepository dataRefreshHistoryRepository)
        {
            this.wmdaHlaVersionProvider = wmdaHlaVersionProvider;
            this.dataRefreshService = dataRefreshService;
            this.dataRefreshHistoryRepository = dataRefreshHistoryRepository;
        }

        public async Task RefreshDataIfNecessary()
        {
            if (ShouldRunDataRefresh())
            {
                await dataRefreshService.RefreshData();
            }
        }

        private bool ShouldRunDataRefresh()
        {
            return HasNewWmdaDataBeenPublished() && !IsRefreshInProgress();
        }

        private bool HasNewWmdaDataBeenPublished()
        {
            var activeHlaDataVersion = wmdaHlaVersionProvider.GetActiveHlaDatabaseVersion();
            var latestHlaDataVersion = wmdaHlaVersionProvider.GetLatestHlaDatabaseVersion();
            return activeHlaDataVersion != latestHlaDataVersion;
        }

        private bool IsRefreshInProgress()
        {
            return dataRefreshHistoryRepository.GetInProgressJobs().Any();
        }
    }
}