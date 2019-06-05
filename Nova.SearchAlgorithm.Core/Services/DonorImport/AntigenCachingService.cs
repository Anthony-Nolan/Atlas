using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Nova.HLAService.Client;
using Nova.HLAService.Client.Models;
using Nova.SearchAlgorithm.Common.Models;
using Nova.Utils.ApplicationInsights;
using Nova.Utils.Models;
using Locus = Nova.SearchAlgorithm.Common.Models.Locus;

namespace Nova.SearchAlgorithm.Services.DonorImport
{
    public interface IAntigenCachingService
    {
        Task GenerateAntigenCache();
    }
    
    public class AntigenCachingService: IAntigenCachingService
    {
        private readonly IHlaServiceClient hlaServiceClient;
        private readonly ILogger logger;
        private readonly IMemoryCache memoryCache;
        private const string CacheKeyAntigens = "Antigens";

        public AntigenCachingService(IMemoryCache memoryCache, ILogger logger, IHlaServiceClient hlaServiceClient)
        {
            this.memoryCache = memoryCache;
            this.logger = logger;
            this.hlaServiceClient = hlaServiceClient;
        }

        public async Task GenerateAntigenCache()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            
            // Create a dummy Phenotypeinfo to make use of its loci helper method
            var dummyPhenotypeInfo = new PhenotypeInfo<int>();
            await dummyPhenotypeInfo.WhenAllLoci(async (locus, hla1, hla2) =>
            {
                var lociStopwatch = new Stopwatch();
                lociStopwatch.Start();

                Enum.TryParse(locus.ToString(), true, out MolecularLocusType locusType);
                var antigens = await hlaServiceClient.GetAntigens((LocusType) locusType);

                logger.SendTrace("Fetched antigens from HLA service", LogLevel.Info, new Dictionary<string, string>
                {
                    {"Locus", locus.ToString()},
                    {"AntigenCount", antigens.Count().ToString()},
                    {"UpdateTime", lociStopwatch.ElapsedMilliseconds.ToString()}
                });

                memoryCache.Set($"{CacheKeyAntigens}_{locus}", antigens.Where(a => a.NmdpString != null).ToDictionary(a => a.NmdpString, a => a.HlaName));
            });
            
            logger.SendTrace("Generated antigen cache", LogLevel.Info, new Dictionary<string, string>
            {
                {"UpdateTime", stopwatch.ElapsedMilliseconds.ToString()}
            });
        }
    }
}