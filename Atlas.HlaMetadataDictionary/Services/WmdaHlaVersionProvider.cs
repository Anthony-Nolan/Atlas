using System;
using System.Linq;
using Atlas.HlaMetadataDictionary.Data;
using Atlas.Common.Caching;
using LazyCache;

namespace Atlas.HlaMetadataDictionary.Services
{
    public interface IWmdaHlaVersionProvider
    {
        /// <summary>
        /// Fetches the last stable hla database version.
        /// </summary>
        /// <returns>The latest stable database version, in the format "3370" (i.e. major & minor versions only, no dots)</returns>
        string GetLatestStableHlaDatabaseVersion();
    }

    public class WmdaHlaVersionProvider : IWmdaHlaVersionProvider
    {
        private readonly IWmdaFileReader fileReader;
        private readonly IAppCache cache;

        public WmdaHlaVersionProvider(
            IWmdaFileReader fileReader,
            ITransientCacheProvider cacheProvider)
        {
            this.fileReader = fileReader;
            this.cache = cacheProvider.Cache;
        }

        public string GetLatestStableHlaDatabaseVersion()
        {
            const string key = "latestWmdaVersion";
            var version = cache.GetOrAdd(key, () =>
            {
                // The currently recommended way of finding out the last version is from the header of the "Allelelist_history.txt" file, 
                // which contains all historic versions of the database
                const string versionId = "Latest";
                const string fileName = "Allelelist_history.txt";

                var versionReportAllLines = fileReader.GetFileContentsWithoutHeader(versionId, fileName);
                var versionLine = versionReportAllLines.Single(line => line.StartsWith("HLA_ID"));
                
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