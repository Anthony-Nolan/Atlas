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
using Azure;
using Azure.Data.Tables;
using Polly;

namespace Atlas.MultipleAlleleCodeDictionary.AzureStorage.Repositories
{
    internal interface IMacRepository
    {
        /// <param name="bypassMetadata">
        /// Defaults to false.
        /// When set, will not use the efficient stored metadata to work out the latest seen MAC, and instead will work it out from the data.
        /// This is *very slow*, and as such should only be done when absolutely necessary, e.g. the metadata has strayed out of sync from the real data.
        /// </param>
        public Task<string> GetLastMacEntry(bool bypassMetadata = false);
        public Task InsertMacs(IEnumerable<Mac> macCodes);
        public Task<Mac> GetMac(string macCode);
        public Task<IReadOnlyCollection<Mac>> GetAllMacs();
        public Task TruncateMacTable();
    }

    internal class MacRepository : IMacRepository
    {
        private readonly ILogger logger;
        protected readonly TableClient Table;

        private readonly string NonMetaDataFilter = TableClient.CreateQueryFilter($"PartitionKey ne {LastStoredMacMetadataEntity.MetadataPartitionKey}");

        public MacRepository(MacDictionarySettings macDictionarySettings, ILogger logger)
        {
            this.logger = logger;
            var connectionString = macDictionarySettings.AzureStorageConnectionString;
            var tableName = macDictionarySettings.TableName;
             
            Table = new TableClient(connectionString, tableName); //TODO: ATLAS-485. Combine this with the CloudTableFactory in HMD.
            Table.CreateIfNotExists(); //TODO: ATLAS-512. Is there any mileage in using the "Lazy" Indexing Policy? (Apparently requires "Gateway" mode on the table?)
        }

        public async Task<string> GetLastMacEntry(bool bypassMetadata)
        {
            if (bypassMetadata)
            {
                return await CalculateLastMacEntry();
            }
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
            if (macEntity == null)
            {
                throw new MacNotFoundException(macCode);
            }

            return new Mac(macEntity);
        }

        public async Task<IReadOnlyCollection<Mac>> GetAllMacs()
        {
            var result = await Table.ExecuteQueryAsync<MacEntity>(NonMetaDataFilter);
            return result.Select(x => new Mac(x)).ToList().AsReadOnly();
        }

        private async Task StoreLatestMacRecord(string latestMac)
        {
            var metadataEntity = new LastStoredMacMetadataEntity
            {
                Code = latestMac
            };

            await Table.UpsertEntityAsync(metadataEntity, TableUpdateMode.Replace);
        }

        /// <summary>
        /// Uses MAC naming convention to work out which imported MAC is latest in the sequence.
        /// This iterates through all known MACs, and as such is very slow.
        /// Should only be used as a backup, when the last imported snapshot is not usable.  
        /// </summary>
        private async Task<string> CalculateLastMacEntry()
        {
            logger.SendTrace("Calculating last seen MAC from all MAC data. If this is called, last seen metadata has probably been deleted.");
            var result = await Table.ExecuteQueryAsync<MacEntity>(NonMetaDataFilter);
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
            await Table.DeleteAsync();


            // According documentation, delete doesn't wait until the deletion is finalised before continuing.
            // So we have to wait for it ourselves.
            var twoMinuteRetryPolicy = Policy
                .Handle<RequestFailedException>(ex =>
                    ex.Status == 409 && 
                    ex.ErrorCode  == "TableBeingDeleted"
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
            var existingMacs =
                await Table.ExecuteQueryAsync<MacEntity>(""); // GetAllMacs would A) do unnecessary conversions and B) skip the LastUpdated record

            foreach (var macToDelete in existingMacs)
            {
                // A TableEntities ETag property must be set to "*" to allow overwrites.
                macToDelete.ETag = new ETag("*");

                await Table.DeleteEntityAsync(macToDelete);
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