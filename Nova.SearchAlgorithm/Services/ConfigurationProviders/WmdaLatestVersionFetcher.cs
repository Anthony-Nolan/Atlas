using System.Linq;
using System.Net;
using Microsoft.Extensions.Options;
using Nova.SearchAlgorithm.Settings;

namespace Nova.SearchAlgorithm.Services.ConfigurationProviders
{
    public interface IWmdaLatestVersionFetcher
    {
        string GetLatestWmdaVersion();
    }
    
    public class WmdaLatestVersionFetcher : IWmdaLatestVersionFetcher
    {
        private readonly string wmdaBaseUrl;
        private readonly WebClient webClient;

        public WmdaLatestVersionFetcher(IOptions<WmdaSettings> wmdaSettings)
        {
            wmdaBaseUrl = wmdaSettings.Value.WmdaFileUri;
            webClient = new WebClient();
        }
        
        public string GetLatestWmdaVersion()
        {
            var versionReport = webClient.DownloadString($"{wmdaBaseUrl}Latest/version_report.txt");
            var versionLine = versionReport.Split('\n').Single(line => line.StartsWith("# version"));
            var version = string.Join("", versionLine.Split(' ').Last().Split('.'));
            return version;
        }
    }
}