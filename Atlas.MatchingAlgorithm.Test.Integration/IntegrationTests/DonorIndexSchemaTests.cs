using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.ConnectionStringProviders;
using AwesomeAssertions;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Injection = Atlas.MatchingAlgorithm.Test.Integration.DependencyInjection.DependencyInjection;

namespace Atlas.MatchingAlgorithm.Test.Integration.IntegrationTests;

/// <summary>
/// Proves the migrations put the covering columns on the Donors indexes that search depends on. The model side
/// of the same contract is asserted by DonorIndexModelTests, in the unit test project; this fixture proves the
/// database agrees with the model.
///
/// Both transient databases are checked. Searches read from whichever one the persistent database marks as
/// active, and a data refresh can change which that is, so a fix applied to only one of them is not a fix.
/// </summary>
[TestFixture]
public class DonorIndexSchemaTests
{
    private const string IncludedColumnsSql = @"
        SELECT c.name
        FROM sys.indexes i
        INNER JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
        INNER JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
        WHERE i.object_id = OBJECT_ID('dbo.Donors')
            AND i.name = @indexName
            AND ic.is_included_column = 1";

    [TestCase(TransientDatabase.DatabaseA)]
    [TestCase(TransientDatabase.DatabaseB)]
    public async Task DonorTypeRegistryCodeIndex_IncludesDonorId(TransientDatabase database)
    {
        var includedColumns = await GetIncludedColumns(database, "IX_Donors_DonorType_RegistryCode");

        includedColumns.Should().BeEquivalentTo(new[] { "DonorId" });
    }

    [TestCase(TransientDatabase.DatabaseA)]
    [TestCase(TransientDatabase.DatabaseB)]
    public async Task DonorIdIndex_IncludesDonorTypeAndRegistryCode(TransientDatabase database)
    {
        var includedColumns = await GetIncludedColumns(database, "IX_DonorId");

        includedColumns.Should().BeEquivalentTo(new[] { "DonorType", "RegistryCode" });
    }

    private static async Task<IEnumerable<string>> GetIncludedColumns(TransientDatabase database, string indexName)
    {
        var connectionString = Injection.Provider
            .GetService<StaticallyChosenTransientSqlConnectionStringProviderFactory>()
            .GenerateConnectionStringProvider(database)
            .GetConnectionString();

        await using var connection = new SqlConnection(connectionString);
        return (await connection.QueryAsync<string>(IncludedColumnsSql, new { indexName })).ToList();
    }
}
