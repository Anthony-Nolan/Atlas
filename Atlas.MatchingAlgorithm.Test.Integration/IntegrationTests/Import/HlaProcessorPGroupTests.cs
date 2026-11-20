using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.HlaMetadataDictionary.ExternalInterface;
using Atlas.HlaMetadataDictionary.Test.IntegrationTests.TestHelpers.FileBackedStorageStubs;
using Atlas.MatchingAlgorithm.Data.Models.DonorInfo;
using Atlas.MatchingAlgorithm.Data.Repositories.DonorRetrieval;
using Atlas.MatchingAlgorithm.Data.Repositories.DonorUpdates;
using Atlas.MatchingAlgorithm.Data.Services;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.ConnectionStringProviders;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Atlas.MatchingAlgorithm.Services.DataRefresh.HlaProcessing;
using Atlas.MatchingAlgorithm.Test.Integration.TestHelpers;
using Atlas.MatchingAlgorithm.Test.Integration.TestHelpers.Builders;
using AwesomeAssertions;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Integration.IntegrationTests.Import;

/// <summary>
/// Runs the HLA processing stage of Data Refresh over a donor in the dormant database. The stage inserts every P-group of the
/// HLA nomenclature version first, and then imports each batch with <c>pGroupsInsertedUpfront: true</c> (ATL-312).
/// </summary>
/// <remarks>
/// The transient databases are cleared first, so that the P-groups in the database are only the ones the stage inserts.
/// </remarks>
public class HlaProcessorPGroupTests : FixtureWithPooledDatabases
{
    private const string HlaNomenclatureVersion = FileBackedHlaMetadataRepositoryBaseReader.OlderTestHlaVersion;

    private IDonorImportRepository importRepo;
    private IDonorInspectionRepository inspectionRepo;
    private IHlaProcessor processor;
    private IHlaMetadataDictionary hlaMetadataDictionary;
    private IConnectionStringProvider dormantConnectionStringProvider;

    [SetUp]
    public void SetUp()
    {
        DependencyInjection.DependencyInjection.NewScope();
        DatabaseManager.ClearTransientDatabases();

        var provider = DependencyInjection.DependencyInjection.Provider;
        importRepo = provider.GetService<IDormantRepositoryFactory>().GetDonorImportRepository();
        // The stage writes to the dormant database, so that is the one to inspect.
        var dormantDatabase = provider.GetService<IActiveDatabaseProvider>().GetDormantDatabase();
        inspectionRepo = provider.GetService<IStaticallyChosenDatabaseRepositoryFactory>()
            .GetDonorInspectionRepositoryForDatabase(dormantDatabase);
        processor = provider.GetService<IHlaProcessor>();
        hlaMetadataDictionary = provider.GetService<IHlaMetadataDictionaryFactory>().BuildDictionary(HlaNomenclatureVersion);
        dormantConnectionStringProvider = provider.GetService<DormantTransientSqlConnectionStringProvider>();
    }

    [Test]
    public async Task UpdateDonorHla_InsertsEveryPGroupOfTheVersionFirst_AndWritesThePGroupsOfTheDonorHla()
    {
        // An allele string with more than one P-group, so that the donor has several relations at the position.
        var donor = new DonorInfoBuilder().WithHlaAtLocus(Locus.A, LocusPosition.One, "*02:01/02:02").Build();
        await importRepo.InsertBatchOfDonors(new List<DonorInfo> { donor });

        await processor.UpdateDonorHla(HlaNomenclatureVersion, _ => Task.CompletedTask);

        var allPGroupsOfVersion = (await hlaMetadataDictionary.GetAllPGroups()).Distinct().ToList();
        (await GetAllPGroupNamesInDatabase()).Should().BeEquivalentTo(allPGroupsOfVersion);

        var expectedPGroups = (await hlaMetadataDictionary.GetLocusHlaMatchingMetadata(Locus.A, donor.HlaNames.A))
            .Position1.MatchingPGroups.ToList();
        expectedPGroups.Should().HaveCountGreaterThan(1, "the test HLA must have several P-groups");

        var storedPGroups = (await inspectionRepo.GetPGroupsForDonors(new[] { donor.DonorId })).Single().PGroupNames;
        storedPGroups.A.Position1.Should().BeEquivalentTo(expectedPGroups);
    }

    private async Task<IList<string>> GetAllPGroupNamesInDatabase()
    {
        await using var conn = new SqlConnection(dormantConnectionStringProvider.GetConnectionString());
        return (await conn.QueryAsync<string>("SELECT Name FROM PGroupNames")).ToList();
    }
}
