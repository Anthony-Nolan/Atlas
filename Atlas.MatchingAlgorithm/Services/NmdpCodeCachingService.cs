using LazyCache;
using Atlas.HLAService.Client;
using Atlas.MatchingAlgorithm.Common.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Caching;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.Common.GeneticData.Hla.Services;
using Locus = Atlas.Common.GeneticData.Locus;
using LocusType = Atlas.Common.GeneticData.LocusType;

namespace Atlas.MultipleAlleleCodeDictionary
{
    public interface IAntigenCachingService
    {
        Task GenerateAntigenCache();
    }

    public class NmdpCodeCachingService : IAntigenCachingService, INmdpCodeCache
    {
        private const string CacheKeyPrefix = "NmdpCodeLookup";

        private readonly IAppCache cache;
        private readonly ILogger logger;
        private readonly IHlaServiceClient hlaServiceClient;
        private readonly IHlaCategorisationService categorisationService;
        private readonly IAlleleStringSplitterService alleleSplitter;

        public NmdpCodeCachingService(
            IPersistentCacheProvider cacheProvider,
            ILogger logger,
            IHlaServiceClient hlaServiceClient,
            IHlaCategorisationService categorisationService,
            IAlleleStringSplitterService alleleSplitter)
        {
            this.cache = cacheProvider.Cache;
            this.logger = logger;
            this.hlaServiceClient = hlaServiceClient;
            this.categorisationService = categorisationService;
            this.alleleSplitter = alleleSplitter;
        }

        public async Task<IEnumerable<string>> GetOrAddAllelesForNmdpCode(Locus locus, string nmdpCode)
        {
            if (!IsNmdpCode(nmdpCode))
            {
                throw new ArgumentException($"{nmdpCode} is not a valid NMDP code (submitted at locus: {locus}).");
            }

            var nmdpCodeLookup = await GetNmdpCodeLookup(locus);

            if (nmdpCodeLookup != null && nmdpCodeLookup.TryGetValue(FormattedNmdpCode(nmdpCode), out var alleles))
            {
                return alleles;
            }

            logger.SendTrace("Failed to lookup nmdp code from cache", LogLevel.Warn, new Dictionary<string, string>
            {
                {"LocusName", locus.ToString()},
                {"LookupName", nmdpCode}
            });

            return await GetAndAddAllelesForNmdpCode(locus, nmdpCode);
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
                cache.Add(LocusCacheKey(locus), nmdpCodeLookup);
            });

            logger.SendTrace("Generated antigen cache", LogLevel.Info, new Dictionary<string, string>
            {
                {"UpdateTime", stopwatch.ElapsedMilliseconds.ToString()}
            });
        }

        private async Task<Dictionary<string, IEnumerable<string>>> GetNmdpCodeLookup(Locus locus)
        {
            return await cache.GetOrAddAsync(LocusCacheKey(locus), () => FetchNmdpCodeLookup(locus));
        }

        private async Task<IEnumerable<string>> GetAndAddAllelesForNmdpCode(Locus locus, string nmdpCode)
        {
            Enum.TryParse(locus.ToString(), true, out LocusType molecularLocusType);
            var alleles = await hlaServiceClient.GetAllelesForDefinedNmdpCode(molecularLocusType, nmdpCode);
            await UpdateNmdpCodeLookup(locus, nmdpCode, alleles);

            return alleles;
        }

        private async Task UpdateNmdpCodeLookup(Locus locus, string nmdpCode, IEnumerable<string> alleles)
        {
            var existingCache = await GetNmdpCodeLookup(locus);

            if (existingCache == null || !existingCache.TryAdd(FormattedNmdpCode(nmdpCode), alleles))
            {
                logger.SendTrace("Failed to add nmdp code to cache", LogLevel.Warn, new Dictionary<string, string>
                {
                    {"LocusName", locus.ToString()},
                    {"NmdpCode", nmdpCode}
                });
            }
        }

        private static string LocusCacheKey(Locus locus)
        {
            return $"{CacheKeyPrefix}_{locus}";
        }

        private static string FormattedNmdpCode(string nmdpCode)
        {
            const string prefix = "*";
            return nmdpCode.StartsWith(prefix)
                ? nmdpCode
                : prefix + nmdpCode;
        }

        private async Task<Dictionary<string, IEnumerable<string>>> FetchNmdpCodeLookup(Locus locus)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            Enum.TryParse(locus.ToString(), true, out LocusType locusType);
            var antigens = await hlaServiceClient.GetAntigens(locusType);

            logger.SendTrace("Fetched antigens from HLA service", LogLevel.Info, new Dictionary<string, string>
            {
                {"Locus", locus.ToString()},
                {"AntigenCount", antigens.Count.ToString()},
                {"UpdateTime", stopwatch.ElapsedMilliseconds.ToString()}
            });

            return antigens
                .Where(a => IsNmdpCode(a.NmdpString))
                .Select(a => new { NmdpCode = a.NmdpString, Alleles = GetAllelesFromHlaName(a.HlaName) })
                .Where(a => a.Alleles != null)
                .ToDictionary(a => a.NmdpCode, a => a.Alleles);
        }

        private bool IsNmdpCode(string value)
        {
            try
            {
                return value != null && categorisationService.GetHlaTypingCategory(value) == HlaTypingCategory.NmdpCode;
            }
            catch (Exception ex)
            {
                logger.SendTrace("Failed to categorise nmdp string value.", LogLevel.Warn, new Dictionary<string, string>
                {
                    {"NmdpString", value},
                    {"Exception", ex.ToString()}
                });

                return false;
            }
        }

        private IEnumerable<string> GetAllelesFromHlaName(string hlaName)
        {
            try
            {
                return alleleSplitter.GetAlleleNamesFromAlleleString(hlaName);
            }
            catch (Exception ex)
            {
                logger.SendTrace("Failed to split allele string.", LogLevel.Warn, new Dictionary<string, string>
                {
                    {"AlleleString", hlaName},
                    {"Exception", ex.ToString()}
                });

                return default;
            }
        }
    }
}