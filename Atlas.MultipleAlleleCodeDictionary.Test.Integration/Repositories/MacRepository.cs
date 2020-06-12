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
        
        // TODO: ATLAS-47: Remove this in favour of non-test version
        public Task<IEnumerable<MacEntity>> GetAllMacEntities();

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
            foreach (var alleleCodeEntity in await GetAllMacEntities())
            {
                var delete = TableOperation.Delete(alleleCodeEntity);
                Table.Execute(delete);
            }
        }

        /// <inheritdoc />
        public async Task<IEnumerable<MacEntity>> GetAllMacEntities()
        {
            var query = new TableQuery<MacEntity>();
            return await Table.ExecuteQueryAsync(query);
        }
    }
}