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
/// Proves the migrations set <c>OPTIMIZE_FOR_SEQUENTIAL_KEY</c> as intended on the primary keys of the two genotype set
/// tables: ON for <c>DonorSubjectGenotypeSets</c> (ATL-472), OFF for <c>SubjectGenotypeSetValues</c> (ATL-491). EF Core
/// cannot declare the option, so no model test covers it: this is the only check.
///
/// Both transient databases are checked, as the data refresh writes to whichever one is dormant.
/// </summary>
[TestFixture]
public class GenotypeSetPrimaryKeySchemaTests
{
    private const string OptimizeForSequentialKeySql = @"
        SELECT i.optimize_for_sequential_key
        FROM sys.indexes i
        WHERE i.object_id = OBJECT_ID(@tableName)
            AND i.name = @indexName
            AND i.is_primary_key = 1";

    [TestCase(TransientDatabase.DatabaseA, "dbo.SubjectGenotypeSetValues", "PK_SubjectGenotypeSetValues", false)]
    [TestCase(TransientDatabase.DatabaseB, "dbo.SubjectGenotypeSetValues", "PK_SubjectGenotypeSetValues", false)]
    [TestCase(TransientDatabase.DatabaseA, "dbo.DonorSubjectGenotypeSets", "PK_DonorSubjectGenotypeSets", true)]
    [TestCase(TransientDatabase.DatabaseB, "dbo.DonorSubjectGenotypeSets", "PK_DonorSubjectGenotypeSets", true)]
    public async Task PrimaryKey_HasTheIntendedOptimizeForSequentialKey(
        TransientDatabase database,
        string tableName,
        string indexName,
        bool expectedOptimizeForSequentialKey)
    {
        var connectionString = Injection.Provider
            .GetService<StaticallyChosenTransientSqlConnectionStringProviderFactory>()
            .GenerateConnectionStringProvider(database)
            .GetConnectionString();

        await using var connection = new SqlConnection(connectionString);
        var optimizeForSequentialKey = await connection.QuerySingleOrDefaultAsync<bool?>(
            OptimizeForSequentialKeySql, new { tableName, indexName });

        optimizeForSequentialKey.Should().Be(expectedOptimizeForSequentialKey);
    }
}
