using System;
using Atlas.Common.Caching;
using Atlas.HlaMetadataDictionary.Data;
using LazyCache;

namespace Atlas.HlaMetadataDictionary.Services
{
    internal interface IWmdaHlaVersionProvider
    {
        /// <summary>
        /// Fetches the last stable hla database version.
        /// </summary>
        /// <returns>The latest stable database version, in the format "3370" (i.e. major & minor versions only, no dots)</returns>
        string GetLatestStableHlaDatabaseVersion();
    }

    internal class WmdaHlaVersionProvider : IWmdaHlaVersionProvider
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

                var versionLine = fileReader.GetFirstNonCommentLine(versionId, fileName);
                if (!versionLine.StartsWith("HLA_ID"))
                {
                    throw new Exception($"Expected first non-comment line of {fileName} to begin with HLA_ID, but it did not.");
                }
                
                // The first item in the header line is the name, "HLA_ID". Then the versions are listed in reverse chronological order, separated by ",".
                // So the second item is the latest version
                return versionLine.Split(",")[1];
            });
            ThrowIfNull(version, key);
            return version;
        }

        private static void ThrowIfNull(string wmdaDatabaseVersion, string key)
        {
            if (string.IsNullOrWhiteSpace(wmdaDatabaseVersion))
            {
                throw new ArgumentNullException(nameof(wmdaDatabaseVersion),
                    $"Attempted to retrieve the {key}, but found <{wmdaDatabaseVersion}>. This is never an appropriate value, under any circumstances, and would definitely cause myriad problems elsewhere.");
            }
        }
    }
}