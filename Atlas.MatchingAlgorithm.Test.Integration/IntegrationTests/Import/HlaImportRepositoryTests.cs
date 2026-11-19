using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata;
using Atlas.MatchingAlgorithm.Data.Exceptions;
using Atlas.MatchingAlgorithm.Data.Models.DonorInfo;
using Atlas.MatchingAlgorithm.Data.Models.Entities;
using Atlas.MatchingAlgorithm.Data.Repositories;
using Atlas.MatchingAlgorithm.Data.Services;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.ConnectionStringProviders;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Atlas.MatchingAlgorithm.Test.Integration.TestHelpers;
using Atlas.MatchingAlgorithm.Test.Integration.TestHelpers.Builders;
using AwesomeAssertions;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Integration.IntegrationTests.Import;

/// <summary>
/// Data Refresh inserts every P-group of the HLA nomenclature version before HLA processing starts, and then imports each
/// batch with <c>pGroupsInsertedUpfront: true</c>, which skips the per-batch check for new P-groups (ATL-312).
/// </summary>
/// <remarks>
/// Every name is new to the database, so that relations already written by other tests cannot hide what this import writes.
/// <see cref="HlaProcessorPGroupTests"/> runs the whole HLA processing stage, with the upfront insert.
/// </remarks>
[NonParallelizable]
public class HlaImportRepositoryTests
{
    private IDormantRepositoryFactory repositoryFactory;
    private IConnectionStringProvider dormantConnectionStringProvider;

    [SetUp]
    public void SetUp()
    {
        repositoryFactory = DependencyInjection.DependencyInjection.Provider.GetService<IDormantRepositoryFactory>();
        dormantConnectionStringProvider = DependencyInjection.DependencyInjection.Provider
            .GetService<DormantTransientSqlConnectionStringProvider>();
    }

    [Test]
    public async Task ImportHla_WithPGroupsInsertedUpfront_WritesARelationForEachPGroupOfTheHla()
    {
        var hlaName = NewName();
        var pGroups = new List<string> { NewName(), NewName() };
        await repositoryFactory.GetPGroupRepository().InsertPGroups(pGroups);

        var hlaLookup = await repositoryFactory.GetHlaImportRepository()
            .ImportHla(new List<DonorInfoWithExpandedHla> { DonorWithHlaAtA(hlaName, pGroups) }, pGroupsInsertedUpfront: true);

        var relatedPGroups = await GetPGroupsRelatedToHlaAtA(hlaLookup[hlaName]);
        relatedPGroups.Should().BeEquivalentTo(pGroups);
    }

    [Test]
    public async Task ImportHla_WithPGroupsInsertedUpfront_WhenPGroupIsNotInDatabase_ThrowsMissingPGroupException()
    {
        var missingPGroup = NewName();
        var donor = DonorWithHlaAtA(NewName(), new List<string> { missingPGroup });

        var act = () => repositoryFactory.GetHlaImportRepository()
            .ImportHla(new List<DonorInfoWithExpandedHla> { donor }, pGroupsInsertedUpfront: true);

        await act.Should().ThrowAsync<MissingPGroupException>().WithMessage($"*{missingPGroup}*");

        (await GetPGroupIds(missingPGroup)).Should().BeEmpty("the P-group must not be inserted silently");
    }

    [Test]
    public async Task ImportHla_WithoutPGroupsInsertedUpfront_InsertsNewPGroupsAndWritesTheirRelations()
    {
        var hlaName = NewName();
        var pGroups = new List<string> { NewName(), NewName() };

        var hlaLookup = await repositoryFactory.GetHlaImportRepository()
            .ImportHla(new List<DonorInfoWithExpandedHla> { DonorWithHlaAtA(hlaName, pGroups) }, pGroupsInsertedUpfront: false);

        var relatedPGroups = await GetPGroupsRelatedToHlaAtA(hlaLookup[hlaName]);
        relatedPGroups.Should().BeEquivalentTo(pGroups);
    }

    /// <summary>
    /// Short, so that it fits the name columns, and unique, so that it is new to the database.
    /// </summary>
    private static string NewName() => $"T{Guid.NewGuid():N}"[..16];

    private static DonorInfoWithExpandedHla DonorWithHlaAtA(string hlaName, IList<string> pGroups)
    {
        var hla = new TestHlaMetadata { LookupName = hlaName, MatchingPGroups = pGroups };
        return new DonorInfoWithExpandedHla
        {
            DonorId = DonorIdGenerator.NextId(),
            MatchingHla = new PhenotypeInfo<INullHandledHlaMatchingMetadata>().SetPosition(Locus.A, LocusPosition.One, hla)
        };
    }

    private async Task<IList<string>> GetPGroupsRelatedToHlaAtA(int hlaNameId)
    {
        var sql = $@"
            SELECT p.{nameof(PGroupName.Name)}
            FROM {HlaNamePGroupRelation.TableName(Locus.A)} r
            JOIN PGroupNames p ON p.{nameof(PGroupName.Id)} = r.{nameof(HlaNamePGroupRelation.PGroupId)}
            WHERE r.{nameof(HlaNamePGroupRelation.HlaNameId)} = @hlaNameId";

        await using var conn = new SqlConnection(dormantConnectionStringProvider.GetConnectionString());
        return (await conn.QueryAsync<string>(sql, new { hlaNameId })).ToList();
    }

    private async Task<IList<int>> GetPGroupIds(string pGroupName)
    {
        await using var conn = new SqlConnection(dormantConnectionStringProvider.GetConnectionString());
        return (await conn.QueryAsync<int>(
            $"SELECT {nameof(PGroupName.Id)} FROM PGroupNames WHERE {nameof(PGroupName.Name)} = @pGroupName",
            new { pGroupName })).ToList();
    }
}
