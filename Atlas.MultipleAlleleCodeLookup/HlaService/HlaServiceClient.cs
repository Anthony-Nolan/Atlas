using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.GeneticData;
using Atlas.Common.NovaHttpClient;
using Atlas.Common.NovaHttpClient.Client;

namespace Atlas.MultipleAlleleCodeDictionary.HLAService
{
    public interface IHlaServiceClient
    {
        Task<List<Antigen>> GetAntigens(Locus locus, bool shouldResetCache = false);
        Task<List<string>> GetAllelesForDefinedNmdpCode(Locus locus, string nmdpCode);
    }

    public class HlaServiceClient : ClientBase, IHlaServiceClient
    {
        public HlaServiceClient(HttpClientSettings settings, ILogger logger) : base(settings, logger)
        {
        }

        public async Task<List<Antigen>> GetAntigens(Locus locus, bool shouldResetCache)
        {
            var request = GetRequest(HttpMethod.Get, $"antigens", new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("locusType", locus.ToString()),
                new KeyValuePair<string, string>("shouldResetCache", shouldResetCache.ToString())
            });
            return await MakeRequestAsync<List<Antigen>>(request);
        }

        public async Task<List<string>> GetAllelesForDefinedNmdpCode(Locus locus, string nmdpCode)
        {
            var request = GetRequest(HttpMethod.Get, "antigen-alleles-defined", new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("locusType", locus.ToString()),
                new KeyValuePair<string, string>("nmdpCode", nmdpCode),
            });
            return await MakeRequestAsync<List<string>>(request);
        }
    }
}