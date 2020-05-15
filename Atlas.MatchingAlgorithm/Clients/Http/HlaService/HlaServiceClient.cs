using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Atlas.MatchingAlgorithm.Clients.Http.HlaService.Models;
using Atlas.Utils.Core.ApplicationInsights;
using Atlas.Utils.Core.Models;
using Atlas.Utils.NovaHttpClient;

namespace Atlas.HLAService.Client
{
    public interface IHlaServiceClient
    {
        Task<List<Antigen>> GetAntigens(LocusType locusType, bool shouldResetCache = false);
        Task<List<string>> GetAllelesForDefinedNmdpCode(LocusType locusType, string nmdpCode);
    }

    public class HlaServiceClient : ClientBase, IHlaServiceClient
    {
        public HlaServiceClient(HttpClientSettings settings, ILogger logger) : base(settings, logger)
        {
        }

        public async Task<List<Antigen>> GetAntigens(LocusType locusType, bool shouldResetCache)
        {
            var request = GetRequest(HttpMethod.Get, $"antigens", new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("locusType", locusType.ToString()),
                new KeyValuePair<string, string>("shouldResetCache", shouldResetCache.ToString())
            });
            return await MakeRequestAsync<List<Antigen>>(request);
        }

        public async Task<List<string>> GetAllelesForDefinedNmdpCode(LocusType locusType, string nmdpCode)
        {
            var request = GetRequest(HttpMethod.Get, "antigen-alleles-defined", new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("locusType", locusType.ToString()),
                new KeyValuePair<string, string>("nmdpCode", nmdpCode),
            });
            return await MakeRequestAsync<List<string>>(request);
        }
    }
}