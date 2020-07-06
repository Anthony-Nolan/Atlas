using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.AzureStorage.TableStorage;
using Atlas.MultipleAlleleCodeDictionary.AzureStorage.Models;
using Atlas.MultipleAlleleCodeDictionary.ExternalInterface.Models;
using Atlas.MultipleAlleleCodeDictionary.Services;
using Atlas.MultipleAlleleCodeDictionary.Settings;
using Microsoft.Azure.Cosmos.Table;
using QueryComparisons = Microsoft.WindowsAzure.Storage.Table.QueryComparisons;

namespace Atlas.MultipleAlleleCodeDictionary.AzureStorage.Repositories
{
    internal interface IMacRepository
    {
        public Task<string> GetLastMacEntry();
        public Task InsertMacs(IEnumerable<Mac> macCodes);
        public Task<Mac> GetMac(string macCode);
        public Task<List<Mac>> GetAllMacs();
    }

    internal class MacRepository : IMacRepository
    {
        private readonly ILogger logger;
        protected readonly CloudTable Table;

        public MacRepository(MacDictionarySettings macDictionarySettings, ILogger logger)
        {
            this.logger = logger;
            var connectionString = macDictionarySettings.AzureStorageConnectionString;
            var tableName = macDictionarySettings.TableName;
            var storageAccount = CloudStorageAccount.Parse(connectionString); //TODO: ATLAS-485. Combine this with the CloudTableFactory in HMD.
            var tableClient = storageAccount.CreateCloudTableClient();
            Table = tableClient.GetTableReference(tableName);
            Table.CreateIfNotExists(); //TODO: ATLAS-455. Is there any mileage in using the "Lazy" Indexing Policy? (Apparently requires "Gateway" mode on the table?)
        }

        public async Task<string> GetLastMacEntry()
        {
            var lastMacEntryFromMetadata = await FetchLastMacEntryFromMetadata();
            return lastMacEntryFromMetadata ?? await CalculateLastMacEntry();
        }

        public async Task InsertMacs(IEnumerable<Mac> macs)
        {
            var orderedMacs = macs.InOrderOfDefinition().ToList();
            if (!orderedMacs.Any())
            {
                return;
            }

            var latestMac = orderedMacs.First();
            var macEntities = orderedMacs.Select(mac => new MacEntity(mac));
            await Table.BatchInsert(macEntities);
            await StoreLatestMacRecord(latestMac.Code);
        }

        public async Task<Mac> GetMac(string macCode)
        {
            var macEntity = await Table.GetByPartitionAndRowKey<MacEntity>(macCode.AsPartitionKey(), macCode.AsRowKey());
            return new Mac(macEntity);
        }

        public async Task<List<Mac>> GetAllMacs()
        {
            var nonMetadataFilter = TableQuery.GenerateFilterCondition(
                "PartitionKey",
                QueryComparisons.NotEqual,
                LastStoredMacMetadataEntity.MetadataPartitionKey
            );

            var query = new TableQuery<MacEntity>().Where(nonMetadataFilter);
            var result = await Table.ExecuteQueryAsync(query);
            return result.Select(x => new Mac(x)).ToList();
        }

        private async Task StoreLatestMacRecord(string latestMac)
        {
            var metadataEntity = new LastStoredMacMetadataEntity
            {
                Code = latestMac
            };
            var operation = TableOperation.InsertOrReplace(metadataEntity);
            await Table.ExecuteAsync(operation);
        }

        /// <summary>
        /// Uses MAC naming convention to work out which imported MAC is latest in the sequence.
        /// This iterates through all known MACs, and as such is very slow.
        /// Should only be used as a backup, when the last imported snapshot is not usable.  
        /// </summary>
        private async Task<string> CalculateLastMacEntry()
        {
            logger.SendTrace("Calculating last seen MAC from all MAC data. If this is called, last seen metadata has probably been deleted.");
            var query = new TableQuery<MacEntity>();
            var result = await Table.ExecuteQueryAsync(query);
            var latestMac = result.InOrderOfDefinition().FirstOrDefault()?.Code;
            await StoreLatestMacRecord(latestMac);
            return latestMac;
        }

        private async Task<string> FetchLastMacEntryFromMetadata()
        {
            var metadataEntity = (await Table.GetByPartitionAndRowKey<LastStoredMacMetadataEntity>(
                LastStoredMacMetadataEntity.MetadataPartitionKey,
                LastStoredMacMetadataEntity.LatestMacRowKey
            ));

            return metadataEntity?.Code;
        }
    }

    internal static class SortingExtension
    {
        /// <summary>
        /// MACs are alphabetical within a character length - any new MACs are appended to the end of the list alphabetically.
        /// Order by length ensure that longer MACs are returned later.
        /// e.g. purely alphabetically, ABC comes before ZX, but as it has fewer character, ZX is actually the earlier MAC
        /// </summary>
        public static IOrderedEnumerable<THasMacCode> InOrderOfDefinition<THasMacCode>(this IEnumerable<THasMacCode> macs)
            where THasMacCode : IHasMacCode
        {
            return macs
                // Note that this is NOT semantically the same as the partition Key! This is an international agreement between biologists about how MAC codes are defined. The PartitionKey is our personal decision about what we think will make a DB query quick!
                .OrderByDescending(x => x.Code.Length)
                .ThenByDescending(x => x.Code);
        }
    }
}