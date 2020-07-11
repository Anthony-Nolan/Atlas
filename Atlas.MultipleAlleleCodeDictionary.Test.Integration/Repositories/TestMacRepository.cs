using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.AzureStorage.TableStorage;
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
        public TestMacRepository(MacDictionarySettings macDictionarySettings, ILogger logger) : base(macDictionarySettings, logger)
        {
        }

        /// <inheritdoc />
        public async Task CreateTableIfNotExists()
        {
            await Table.CreateIfNotExistsAsync();
        }

        /// <remarks>
        /// Don't bother with the underlying Repo's "GetAllMacs" method, as that converts to the domain object first.
        /// If there's ever invalid data stored in the Table from a prior test, then the conversion may b0rk and the
        /// tests will spuriously fail.
        /// </remarks>
        public async Task DeleteAllMacs()
        {
            var query = new TableQuery<MacEntity>();
            var existingMacs = await Table.ExecuteQueryAsync(query);

            foreach (var macToDelete in existingMacs)
            {
                // A TableEntities ETag property must be set to "*" to allow overwrites.
                macToDelete.ETag = "*";
                var delete = TableOperation.Delete(macToDelete);
                Table.Execute(delete);
            }
        }
    }
}