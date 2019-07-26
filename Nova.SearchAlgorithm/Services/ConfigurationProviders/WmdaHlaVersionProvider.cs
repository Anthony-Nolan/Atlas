using System.Linq;
using System.Net;
using LazyCache;
using Microsoft.Extensions.Options;
using Nova.SearchAlgorithm.Data.Persistent.Repositories;
using Nova.SearchAlgorithm.Settings;

namespace Nova.SearchAlgorithm.Services.ConfigurationProviders
{
    public interface IWmdaHlaVersionProvider
    {
        /// <returns>The version of the wmda hla data currently in use by the algorithm</returns>
        string GetActiveHlaDatabaseVersion();

        /// <returns>The latest published version of the wmda hla data</returns>
        string GetLatestHlaDatabaseVersion();
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
            IAppCache cache)
        {
            wmdaBaseUrl = wmdaSettings.Value.WmdaFileUri;
            webClient = new WebClient();
            this.dataRefreshHistoryRepository = dataRefreshHistoryRepository;
            this.cache = cache;
        }

        public string GetActiveHlaDatabaseVersion()
        {
            return cache.GetOrAdd("activeWmdaVersion", () => dataRefreshHistoryRepository.GetActiveWmdaDataVersion());
        }

        public string GetLatestHlaDatabaseVersion()
        {
            return cache.GetOrAdd("latestWmdaVersion", () =>
            {
                var versionReport = webClient.DownloadString($"{wmdaBaseUrl}Latest/version_report.txt");
                var versionLine = versionReport.Split('\n').Single(line => line.StartsWith("# version"));
                return string.Join("", versionLine.Split(' ').Last().Split('.'));
            });
        }
    }
}