using System.Threading.Tasks;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Data.Repositories.Precompute;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.ConnectionStringProviders;
using AwesomeAssertions;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Injection = Atlas.MatchingAlgorithm.Test.Integration.DependencyInjection.DependencyInjection;

namespace Atlas.MatchingAlgorithm.Test.Integration.IntegrationTests;

/// <summary>
/// Proves the migrations give the primary keys of the two genotype set tables the intended shape. EF Core can declare
/// neither the option nor the partitions, so no model test covers them: this is the only check.
///
/// <para>
/// <c>OPTIMIZE_FOR_SEQUENTIAL_KEY</c> is OFF on both keys (ATL-491, ATL-495). Each key is <c>(Id, Bucket)</c>, split into
/// <see cref="SubjectGenotypeSetRepository.BucketCount"/> hash partitions on <c>Bucket = Id % BucketCount</c>, and each
/// unique index is not partitioned, so it stays unique over the whole table (ATL-495).
/// </para>
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
    [TestCase(TransientDatabase.DatabaseA, "dbo.DonorSubjectGenotypeSets", "PK_DonorSubjectGenotypeSets", false)]
    [TestCase(TransientDatabase.DatabaseB, "dbo.DonorSubjectGenotypeSets", "PK_DonorSubjectGenotypeSets", false)]
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

    private const string KeyColumnsSql = @"
        SELECT c.name
        FROM sys.index_columns ic
        JOIN sys.indexes i ON i.object_id = ic.object_id AND i.index_id = ic.index_id
        JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
        WHERE i.object_id = OBJECT_ID(@tableName) AND i.name = @indexName AND ic.key_ordinal > 0
        ORDER BY ic.key_ordinal";

    private const string PartitionCountSql = @"
        SELECT COUNT(*)
        FROM sys.partitions p
        JOIN sys.indexes i ON i.object_id = p.object_id AND i.index_id = p.index_id
        WHERE i.object_id = OBJECT_ID(@tableName) AND i.name = @indexName";

    private const string BucketColumnSql = @"
        SELECT cc.definition
        FROM sys.computed_columns cc
        WHERE cc.object_id = OBJECT_ID(@tableName) AND cc.name = 'Bucket' AND cc.is_persisted = 1";

    [TestCase(TransientDatabase.DatabaseA, "dbo.SubjectGenotypeSetValues", "PK_SubjectGenotypeSetValues",
        "IX_SubjectGenotypeSetValues_HlaTypingKey_HaplotypeFrequencySetId_AllowedLociKey")]
    [TestCase(TransientDatabase.DatabaseB, "dbo.SubjectGenotypeSetValues", "PK_SubjectGenotypeSetValues",
        "IX_SubjectGenotypeSetValues_HlaTypingKey_HaplotypeFrequencySetId_AllowedLociKey")]
    [TestCase(TransientDatabase.DatabaseA, "dbo.DonorSubjectGenotypeSets", "PK_DonorSubjectGenotypeSets",
        "IX_DonorSubjectGenotypeSets_DonorId_AllowedLociKey")]
    [TestCase(TransientDatabase.DatabaseB, "dbo.DonorSubjectGenotypeSets", "PK_DonorSubjectGenotypeSets",
        "IX_DonorSubjectGenotypeSets_DonorId_AllowedLociKey")]
    public async Task PrimaryKey_IsHashPartitionedOnBucket_AndTheUniqueIndexIsNot(
        TransientDatabase database,
        string tableName,
        string indexName,
        string uniqueIndexName)
    {
        await using var connection = new SqlConnection(ConnectionString(database));

        (await connection.QueryAsync<string>(KeyColumnsSql, new { tableName, indexName }))
            .Should().Equal("Id", "Bucket");
        (await connection.ExecuteScalarAsync<string>(BucketColumnSql, new { tableName }))
            .Should().Be($"(CONVERT([tinyint],[Id]%({SubjectGenotypeSetRepository.BucketCount})))");
        (await connection.ExecuteScalarAsync<int>(PartitionCountSql, new { tableName, indexName }))
            .Should().Be(SubjectGenotypeSetRepository.BucketCount);
        (await connection.ExecuteScalarAsync<int>(PartitionCountSql, new { tableName, indexName = uniqueIndexName }))
            .Should().Be(1);
    }

    private static string ConnectionString(TransientDatabase database) => Injection.Provider
        .GetService<StaticallyChosenTransientSqlConnectionStringProviderFactory>()
        .GenerateConnectionStringProvider(database)
        .GetConnectionString();
}
