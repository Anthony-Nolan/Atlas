using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.AzureStorage.TableStorage.Extensions;
using Atlas.MultipleAlleleCodeDictionary.AzureStorage.Models;
using Atlas.MultipleAlleleCodeDictionary.AzureStorage.Repositories;
using Atlas.MultipleAlleleCodeDictionary.Settings;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Options;

namespace Atlas.MultipleAlleleCodeDictionary.Test.Integration.Repositories
{
    internal interface ITestMacRepository : IMacRepository
    {
        public Task CreateTableIfNotExists();
        public Task DeleteAllMacs();
        
        // TODO: ATLAS-47: Remove this in favour of non-test version
        public Task<IEnumerable<MultipleAlleleCodeEntity>> GetAllMacs();

    }

    internal class TestMacRepository : MacRepository, ITestMacRepository
    {
        public TestMacRepository(IOptions<MacImportSettings> macImportSettings) : base(macImportSettings)
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
            foreach (var alleleCodeEntity in await GetAllMacs())
            {
                var delete = TableOperation.Delete(alleleCodeEntity);
                Table.Execute(delete);
            }
        }

        /// <inheritdoc />
        public async Task<IEnumerable<MultipleAlleleCodeEntity>> GetAllMacs()
        {
            var query = new TableQuery<MultipleAlleleCodeEntity>();
            return await Table.ExecuteQueryAsync(query);
        }
    }
}