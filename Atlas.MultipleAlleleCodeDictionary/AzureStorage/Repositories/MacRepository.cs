using System;
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
using Polly;
using QueryComparisons = Microsoft.WindowsAzure.Storage.Table.QueryComparisons;

namespace Atlas.MultipleAlleleCodeDictionary.AzureStorage.Repositories
{
    internal interface IMacRepository
    {
        public Task<string> GetLastMacEntry();
        public Task InsertMacs(IEnumerable<Mac> macCodes);
        public Task<Mac> GetMac(string macCode);
        public Task<IReadOnlyCollection<Mac>> GetAllMacs();
        public Task TruncateMacTable();
    }

    internal class MacRepository : IMacRepository
    {
        private readonly ILogger logger;
        protected readonly CloudTable Table;
        private readonly string NonMetaDataFilter = TableQuery.GenerateFilterCondition(        
        "PartitionKey",                                                
        QueryComparisons.NotEqual,                                     
        LastStoredMacMetadataEntity.MetadataPartitionKey               
        );                                                                 

        public MacRepository(MacDictionarySettings macDictionarySettings, ILogger logger)
        {
            this.logger = logger;
            var connectionString = macDictionarySettings.AzureStorageConnectionString;
            var tableName = macDictionarySettings.TableName;
            var storageAccount = CloudStorageAccount.Parse(connectionString); //TODO: ATLAS-485. Combine this with the CloudTableFactory in HMD.
            var tableClient = storageAccount.CreateCloudTableClient();
            Table = tableClient.GetTableReference(tableName);
            Table.CreateIfNotExists(); //TODO: ATLAS-512. Is there any mileage in using the "Lazy" Indexing Policy? (Apparently requires "Gateway" mode on the table?)
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

            var latestMac = orderedMacs.Last();
            var macEntities = orderedMacs.Select(mac => new MacEntity(mac));
            await Table.BatchInsert(macEntities);
            await StoreLatestMacRecord(latestMac.Code);
        }

        public async Task<Mac> GetMac(string macCode)
        {
            var macEntity = await Table.GetByPartitionAndRowKey<MacEntity>(macCode.AsPartitionKey(), macCode.AsRowKey());
            return new Mac(macEntity);
        }

        public async Task<IReadOnlyCollection<Mac>> GetAllMacs()
        {
            var query = new TableQuery<MacEntity>().Where(NonMetaDataFilter);
            var result = await Table.ExecuteQueryAsync(query);
            return result.Select(x => new Mac(x)).ToList().AsReadOnly();
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
            var query = new TableQuery<MacEntity>().Where(NonMetaDataFilter);
            var result = await Table.ExecuteQueryAsync(query);
            var a = result.InOrderOfDefinition();
            var b = result.InOrderOfDefinition().LastOrDefault();
            var latestMac = result.InOrderOfDefinition().LastOrDefault()?.Code;
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

        /// <remarks>
        /// Rather than attempting to delete each entry, just delete the table and re-create it.
        /// This takes around 30 seconds on 'real' Azure, which is still markedly faster than the "DeleteAll" approach for large tables on Azure. (Which can take hours!)
        /// On the local emulator it's instantaneous, so this isn't an issue for local tests, but it is a noticeable impact on the System Tests run in DevOps against real Azure tables.
        /// </remarks>
        public async Task TruncateMacTable()
        {
            await DeleteAndRecreateTable();
        }

        private async Task DeleteAndRecreateTable()
        {
            await Table.DeleteIfExistsAsync();

            // Weirdly that Async delete doesn't wait until the deletion is finalised before continuing.
            // So we have to wait for it ourselves.
            var twoMinuteRetryPolicy = Policy
                .Handle<StorageException>(ex => 
                    (ex?.RequestInformation?.HttpStatusMessage ?? "") == "Conflict" &&
                    (ex?.RequestInformation?.ExtendedErrorInformation?.ErrorCode ?? "") == "TableBeingDeleted"
                 )
                .WaitAndRetryAsync(120, _ => TimeSpan.FromSeconds(1));

            await twoMinuteRetryPolicy.ExecuteAsync(async () => await Table.CreateIfNotExistsAsync());
        }

        /// <remarks>
        /// Retain this for reference of how to do it correctly, if nothing else.
        /// </remarks>
        // ReSharper disable once UnusedMember.Local
        private async Task ExplicitlyDeleteAllMacRecords()
        {
            var query = new TableQuery<MacEntity>();
            var existingMacs = await Table.ExecuteQueryAsync(query); // GetAllMacs would A) do unnecessary conversions and B) skip the LastUpdated record

            foreach (var macToDelete in existingMacs)
            {
                // A TableEntities ETag property must be set to "*" to allow overwrites.
                macToDelete.ETag = "*";
                var delete = TableOperation.Delete(macToDelete);
                Table.Execute(delete);
            }
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
                .OrderBy(x => x.Code.Length)
                .ThenBy(x => x.Code);
        }
    }
}