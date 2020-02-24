using System.Linq;
using System.Net;
using LazyCache;
using Microsoft.Extensions.Options;
using Nova.SearchAlgorithm.Data.Persistent.Repositories;
using Nova.SearchAlgorithm.Helpers;
using Nova.SearchAlgorithm.Settings;

namespace Nova.SearchAlgorithm.Services.ConfigurationProviders
{
    public interface IWmdaHlaVersionProvider
    {
        /// <returns>The version of the wmda hla data currently in use by the algorithm</returns>
        string GetActiveHlaDatabaseVersion();

        /// <summary>
        /// Fetches the last stable hla database version.
        /// </summary>
        /// <returns>The latest stable database version, in the format "3370" (i.e. major & minor versions only, no dots)</returns>
        string GetLatestStableHlaDatabaseVersion();
    }

    public class WmdaHlaVersionProvider : IWmdaHlaVersionProvider
    {
        private readonly IDataRefreshHistoryRepository dataRefreshHistoryRepository;
        private readonly IAppCache cache;
        private readonly string wmdaBaseUrl;
        private readonly WebClient webClient;

        public WmdaHlaVersionProvider(
            IOptions<WmdaSettings> wmdaSettings,
            IDataRefreshHistoryRepository dataRefreshHistoryRepository,
            ITransientCacheProvider cacheProvider)
        {
            wmdaBaseUrl = wmdaSettings.Value.WmdaFileUri;
            webClient = new WebClient();
            this.dataRefreshHistoryRepository = dataRefreshHistoryRepository;
            cache = cacheProvider.Cache;
        }

        public string GetActiveHlaDatabaseVersion()
        {
            return cache.GetOrAdd("activeWmdaVersion", () => dataRefreshHistoryRepository.GetActiveWmdaDataVersion());
        }

        public string GetLatestStableHlaDatabaseVersion()
        {
            return cache.GetOrAdd("latestWmdaVersion", () =>
            {
                // The currently recommended way of finding out the last version is from the header of the "Allelelist_history.txt" file, 
                // which contains all historic versions of the database
                var versionReport = webClient.DownloadString($"{wmdaBaseUrl}Latest/Allelelist_history.txt");
                var versionLine = versionReport.Split('\n').Single(line => line.StartsWith("HLA_ID"));
                
                // The first item in the header line is the name, "HLA_ID". Then the versions are listed in reverse chronological order.
                // So the second item is the latest version
                return versionLine.Split(",")[1];
            });
        }
    }
}