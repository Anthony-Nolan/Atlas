using System;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Net;
using LazyCache;
using Microsoft.Extensions.Options;
using Atlas.MatchingAlgorithm.Data.Persistent.Repositories;
using Atlas.MatchingAlgorithm.Helpers;
using Atlas.MatchingAlgorithm.ConfigSettings;

namespace Atlas.MatchingAlgorithm.Services.ConfigurationProviders
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
            const string key = "activeWmdaVersion";
            var version = cache.GetOrAdd(key, () => dataRefreshHistoryRepository.GetActiveWmdaDataVersion());
            ThrowIfNull(version, key);
            return version;
        }

        public string GetLatestStableHlaDatabaseVersion()
        {
            const string key = "latestWmdaVersion";
            var version = cache.GetOrAdd("latestWmdaVersion", () =>
            {
                // The currently recommended way of finding out the last version is from the header of the "Allelelist_history.txt" file, 
                // which contains all historic versions of the database
                var versionReport = webClient.DownloadString($"{wmdaBaseUrl}Latest/Allelelist_history.txt");
                var versionLine = versionReport.Split('\n').Single(line => line.StartsWith("HLA_ID"));
                
                // The first item in the header line is the name, "HLA_ID". Then the versions are listed in reverse chronological order.
                // So the second item is the latest version
                return versionLine.Split(",")[1];
            });
            ThrowIfNull(version, key);
            return version;
        }

        private void ThrowIfNull(string wmdaDatabaseVersion, string key)
        {
            if (string.IsNullOrWhiteSpace(wmdaDatabaseVersion))
            {
                throw new ArgumentNullException(nameof(wmdaDatabaseVersion),
                    $"Attempted to retrieve the {key}, but found <{wmdaDatabaseVersion}>. This is never an appropriate value, under any circumstances, and would definitely cause myriad problems elsewhere.");
            }
        }
    }
}