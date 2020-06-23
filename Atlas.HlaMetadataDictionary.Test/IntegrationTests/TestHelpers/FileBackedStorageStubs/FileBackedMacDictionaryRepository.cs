using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Atlas.MultipleAlleleCodeDictionary.AzureStorage.Repositories;
using Atlas.MultipleAlleleCodeDictionary.ExternalInterface.Models;

namespace Atlas.HlaMetadataDictionary.Test.IntegrationTests.TestHelpers.FileBackedStorageStubs
{
    public class FileBackedMacDictionaryRepository : IMacRepository
    {
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
            var macs = await GetAllMacs();
            return macs.Single(mac => mac.Code == macCode);
        }

        public Task<IEnumerable<Mac>> GetAllMacs()
        {
            return Task.FromResult(ReadFile().Select(ParseMac));
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
                using (var stream = assembly.GetManifestResourceStream("Atlas.HlaMetadataDictionary.Test.IntegrationTests.Resources.Mac.csv"))
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