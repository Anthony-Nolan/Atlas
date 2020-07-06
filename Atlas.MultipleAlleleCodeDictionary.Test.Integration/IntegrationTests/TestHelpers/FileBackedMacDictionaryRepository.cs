using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Atlas.Common.Caching;
using Atlas.MultipleAlleleCodeDictionary.AzureStorage.Repositories;
using Atlas.MultipleAlleleCodeDictionary.ExternalInterface.Models;
using LazyCache;

namespace Atlas.MultipleAlleleCodeDictionary.Test.Integration.IntegrationTests.TestHelpers
{
    public class FileBackedMacDictionaryRepository : IMacRepository
    {
        private readonly IAppCache cache;

        public FileBackedMacDictionaryRepository(IPersistentCacheProvider cacheProvider)
        {
            cache = cacheProvider.Cache;
            GetAllMacs();
        }
        public Task<string> GetLastMacEntry()
        {
            throw new System.NotImplementedException();
        }

        public Task InsertMacs(IEnumerable<Mac> macCodes)
        {
            throw new System.NotImplementedException();
        }

        public async Task<Mac> GetMac(string macCode)
        {
            return cache.Get<Mac>(macCode);
        }

        public Task<List<Mac>> GetAllMacs()
        {
            var macs = ReadFile().Select(ParseMac).ToList();
            foreach (var mac in macs)
            {
                cache.GetOrAdd(mac.Code, () => mac);
            }
            return Task.FromResult(macs);
        }

        private static Mac ParseMac(string input)
        {
            var parts = input.Split(',');
            // The csv file is generated from azure storage explorer into a csv with the following format:
            // HLA,HLA@type,PartitionKey,RowKey,Timestamp,isGeneric,isGeneric@type
            // eg: 01/02,Edm.String,3,ABC,2020-06-22T15:16:05.333Z,true,Edm.Boolean
            return new Mac()
            {
                Code = parts[3],
                Hla = parts[0],
                IsGeneric = bool.Parse(parts[5])
            };
        }

        private static IEnumerable<string> ReadFile()
        {
            var assembly = Assembly.GetExecutingAssembly();
                using (var stream = assembly.GetManifestResourceStream("Atlas.MultipleAlleleCodeDictionary.Test.Integration.Resources.Mac.csv"))
                {
                    var stringList = new List<string>();
                    using (var reader = new StreamReader(stream))
                    {
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            stringList.Add(line);
                        }

                        return stringList;
                    }
                }
        }
    }
}