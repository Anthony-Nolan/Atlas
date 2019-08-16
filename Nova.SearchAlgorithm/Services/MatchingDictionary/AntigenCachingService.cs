using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using LazyCache;
using Nova.HLAService.Client;
using Nova.HLAService.Client.Models;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.MatchingDictionary.Caching;
using Nova.Utils.ApplicationInsights;
using Nova.Utils.Models;
using Locus = Nova.SearchAlgorithm.Common.Models.Locus;

namespace Nova.SearchAlgorithm.Services.MatchingDictionary
{
    public interface IAntigenCachingService
    {
        Task GenerateAntigenCache();
    }

    public class AntigenCachingService : IAntigenCachingService, IAntigenCache
    {
        private const string NmdpLookupCacheKeyPrefix = "NmdpCodeLookup";
        
        private readonly IHlaServiceClient hlaServiceClient;
        private readonly ILogger logger;
        private readonly IAppCache cache;

        public AntigenCachingService(IAppCache cache, ILogger logger, IHlaServiceClient hlaServiceClient)
        {
            this.cache = cache;
            this.logger = logger;
            this.hlaServiceClient = hlaServiceClient;
        }

        public async Task<Dictionary<string, string>> GetNmdpCodeLookup(Locus locus)
        {
            return await cache.GetOrAddAsync(AntigenCacheKey(locus), () => FetchNmdpCodeLookup(locus));
        }

        public async Task GenerateAntigenCache()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            // Create a dummy PhenotypeInfo to make use of its loci helper method
            var dummyPhenotypeInfo = new PhenotypeInfo<int>();
            await dummyPhenotypeInfo.WhenAllLoci(async (locus, hla1, hla2) =>
            {
                var nmdpCodeLookup = await FetchNmdpCodeLookup(locus);
                cache.Add(AntigenCacheKey(locus), nmdpCodeLookup);
            });

            logger.SendTrace("Generated antigen cache", LogLevel.Info, new Dictionary<string, string>
            {
                {"UpdateTime", stopwatch.ElapsedMilliseconds.ToString()}
            });
        }

        private static string AntigenCacheKey(Locus locus)
        {
            return $"{NmdpLookupCacheKeyPrefix}_{locus}";
        }

        private async Task<Dictionary<string, string>> FetchNmdpCodeLookup(Locus locus)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            Enum.TryParse(locus.ToString(), true, out MolecularLocusType locusType);
            var antigens = await hlaServiceClient.GetAntigens((LocusType) locusType);

            logger.SendTrace("Fetched antigens from HLA service", LogLevel.Info, new Dictionary<string, string>
            {
                {"Locus", locus.ToString()},
                {"AntigenCount", antigens.Count().ToString()},
                {"UpdateTime", stopwatch.ElapsedMilliseconds.ToString()}
            });

            return antigens.Where(a => a.NmdpString != null).ToDictionary(a => a.NmdpString, a => a.HlaName);
        }
    }
}