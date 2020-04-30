using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Castle.Core.Internal;
using Newtonsoft.Json;
using Atlas.HLAService.Client.Models;
using Nova.Utils.ApplicationInsights;
using Nova.Utils.Client;
using Atlas.Utils.Core.Models;
using Atlas.Utils.Hla.Models;

namespace Atlas.HLAService.Client
{
    public interface IHlaServiceClient
    {
        Task<List<Antigen>> GetAntigens(LocusType locusType, bool shouldResetCache = false);
        Task<int?> GetIdForAntigen(AntigenLookupModel antigenModel);
        Task<Antigen> TryCreateAntigenMappingForNmdpCode(LocusType locus, string nmdpCode);
        Task<List<string>> GetAllelesForDefinedNmdpCode(LocusType locusType, string nmdpCode);
        Task<HlaTypingCategory> GetHlaTypingCategory(string hlaName);
        Task<IEnumerable<string>> GetAlleleNamesFromAlleleString(string alleleString);
        Task<List<Antigen>> GetAntigensByModels(List<AntigenLookupModel> models);
        Task<Dictionary<int, Antigen>> GetAntigensByIds(List<int> ids);
    }

    public class HlaServiceClient : ClientBase, IHlaServiceClient
    {
        public HlaServiceClient(ClientSettings settings, ILogger logger) : base(settings, logger)
        {
        }

        [Obsolete]
        public HlaServiceClient(string baseUrl, string apiKey, JsonSerializerSettings settings)
            : base(new ClientSettings { ApiKey = apiKey, BaseUrl = baseUrl, ClientName = "hla_client", JsonSettings = settings })
        {
        }

        public HlaServiceClient(ClientSettings settings) : base(settings)
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

        public async Task<int?> GetIdForAntigen(AntigenLookupModel antigenModel)
        {
            var parameters = GetAntigenParamters(antigenModel);
            var request = GetRequest(HttpMethod.Get, "antigen-id", parameters);
            return await MakeRequestAsync<int?>(request);
        }

        public async Task<Antigen> TryCreateAntigenMappingForNmdpCode(LocusType locus, string nmdpCode)
        {
            var request = GetRequest(HttpMethod.Post, "antigen-nmdp-mapping", new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("locusType", locus.ToString()),
                new KeyValuePair<string, string>("nmdpCode", nmdpCode),
            });
            return await MakeRequestAsync<Antigen>(request);
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

        private List<KeyValuePair<string, string>> GetAntigenParamters(AntigenLookupModel antigenModel)
        {
            var list = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("LocusType", antigenModel.Locus.ToString())
            };

            if (!antigenModel.NmdpString.IsNullOrEmpty())
                list.Add(new KeyValuePair<string, string>("NmdpString", antigenModel.NmdpString));
            if (!antigenModel.HlaName.IsNullOrEmpty())
                list.Add(new KeyValuePair<string, string>("HlaName", antigenModel.HlaName));

            return list;
        }

        public async Task<List<Antigen>> GetAntigensByModels(List<AntigenLookupModel> models)
        {
            var request = GetRequest(HttpMethod.Post, "antigens-by-model", body: models);
            return await MakeRequestAsync<List<Antigen>>(request);
        }

        public async Task<Dictionary<int, Antigen>> GetAntigensByIds(List<int> ids)
        {
            var request = GetRequest(HttpMethod.Post, "antigens-by-ids", body: ids);
            return await MakeRequestAsync<Dictionary<int, Antigen>>(request);
        }

        public async Task<HlaTypingCategory> GetHlaTypingCategory(string hlaName)
        {
            var request = GetRequest(HttpMethod.Get, "hla-typing-category", new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("hlaName", hlaName)
            });

            return await MakeRequestAsync<HlaTypingCategory>(request);
        }

        public async Task<IEnumerable<string>> GetAlleleNamesFromAlleleString(string alleleString)
        {
            var request = GetRequest(HttpMethod.Get, "alleles-allele-string", new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("alleleString", alleleString)
            });

            return await MakeRequestAsync<IEnumerable<string>>(request);
        }
    }
}