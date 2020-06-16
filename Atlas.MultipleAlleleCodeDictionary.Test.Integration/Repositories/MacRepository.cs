using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.AzureStorage.TableStorage.Extensions;
using Atlas.MultipleAlleleCodeDictionary.AzureStorage.Models;
using Atlas.MultipleAlleleCodeDictionary.AzureStorage.Repositories;
using Atlas.MultipleAlleleCodeDictionary.Settings;
using Microsoft.Azure.Cosmos.Table;

namespace Atlas.MultipleAlleleCodeDictionary.Test.Integration.Repositories
{
    internal interface ITestMacRepository : IMacRepository
    {
        public Task CreateTableIfNotExists();
        public Task DeleteAllMacs();
    }

    internal class TestMacRepository : MacRepository, ITestMacRepository
    {
        public TestMacRepository(MacImportSettings macImportSettings) : base(macImportSettings)
        {
        }

        /// <inheritdoc />
        public async Task CreateTableIfNotExists()
        {
            await Table.CreateIfNotExistsAsync();
        }

        /// <inheritdoc />
        public async Task DeleteAllMacs()
        {
            foreach (var mac in await GetAllMacs())
            {
                var macToDelete = new MacEntity(mac);
                // A TableEntities ETag property must be set to "*" to allow overwrites.
                macToDelete.ETag = "*";
                var delete = TableOperation.Delete(macToDelete);
                Table.Execute(delete);
            }
        }
    }
}