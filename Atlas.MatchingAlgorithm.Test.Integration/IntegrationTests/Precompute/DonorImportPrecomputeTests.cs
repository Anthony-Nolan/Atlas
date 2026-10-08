using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.MatchPrediction;
using Atlas.HlaMetadataDictionary.Test.IntegrationTests.TestHelpers.FileBackedStorageStubs;
using Atlas.MatchingAlgorithm.ApplicationInsights.ContextAwareLogging;
using Atlas.MatchingAlgorithm.Data.Models.DonorInfo;
using Atlas.MatchingAlgorithm.Data.Models.Entities;
using Atlas.MatchingAlgorithm.Data.Models.Precompute;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Data.Repositories;
using Atlas.MatchingAlgorithm.Data.Repositories.DonorRetrieval;
using Atlas.MatchingAlgorithm.Data.Repositories.DonorUpdates;
using Atlas.MatchingAlgorithm.Data.Repositories.Precompute;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.ConnectionStringProviders;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Atlas.MatchingAlgorithm.Services.DataRefresh.Precompute;
using Atlas.MatchingAlgorithm.Services.DonorManagement;
using Atlas.MatchingAlgorithm.Services.Donors;
using Atlas.MatchingAlgorithm.Test.Integration.IntegrationTests.DonorUpdates;
using Atlas.MatchingAlgorithm.Test.Integration.TestHelpers;
using Atlas.MatchingAlgorithm.Test.Integration.TestHelpers.Builders;
using Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using Atlas.MatchPrediction.Services.MatchProbability;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;
using ContextFactory = Atlas.MatchingAlgorithm.Data.Context.ContextFactory;
using Injection = Atlas.MatchingAlgorithm.Test.Integration.DependencyInjection.DependencyInjection;

namespace Atlas.MatchingAlgorithm.Test.Integration.IntegrationTests.Precompute;

/// <summary>
/// ATL-232: genotype set precompute during a differential donor import, end to end against a real database.
/// The Match Prediction pipeline (imputation, frequency set lookup) is substituted; everything that writes is real.
/// </summary>
[TestFixture]
public class DonorImportPrecomputeTests
{
    /// <summary>Not the active database, so the test proves the rows follow the import, not the active database.</summary>
    private const TransientDatabase TargetDatabase = TransientDatabase.DatabaseB;

    private const int LocusCombinations = 4;
    private static readonly string HlaVersion = FileBackedHlaMetadataRepositoryBaseReader.NewerTestsHlaVersion;

    private IGenotypeSetService genotypeSetService;
    private IStaticallyChosenDatabaseRepositoryFactory repositoryFactory;
    private FailingUpsertRepositoryFactory failingUpsertRepositoryFactory;
    private IDonorManagementService service;
    private string targetConnectionString;

    /// <summary>
    /// Once, not per test: the HLA import repositories cache P group and HLA name ids per database, and a truncate
    /// between tests would leave those caches pointing at rows that no longer exist. Each test uses its own donor ids.
    /// </summary>
    [OneTimeSetUp]
    public void OneTimeSetUp() => DatabaseManager.ClearTransientDatabases();

    [SetUp]
    public async Task SetUp()
    {
        targetConnectionString = Injection.Provider.GetService<StaticallyChosenTransientSqlConnectionStringProviderFactory>()
            .GenerateConnectionStringProvider(TargetDatabase).GetConnectionString();

        // The precompute tables only, which no cache holds: a value stored by an earlier test would otherwise be reused,
        // and a test that forces imputation to fail would find nothing left to impute.
        await using (var context = new ContextFactory().Create(targetConnectionString))
        {
            await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE DonorSubjectGenotypeSets; TRUNCATE TABLE SubjectGenotypeSetValues;");
        }

        genotypeSetService = Substitute.For<IGenotypeSetService>();
        genotypeSetService.GetGenotypeSet(default, default)
            .ReturnsForAnyArgs(new SubjectGenotypeSet(true, new List<GenotypeAtDesiredResolutions>(), 0));

        repositoryFactory = Injection.Provider.GetService<IStaticallyChosenDatabaseRepositoryFactory>();
        failingUpsertRepositoryFactory = new FailingUpsertRepositoryFactory(repositoryFactory);

        var frequencyLookupService = Substitute.For<IHaplotypeFrequencyLookupService>();
        frequencyLookupService.GetSingleHaplotypeFrequencySet(default).ReturnsForAnyArgs(new HaplotypeFrequencySet { Id = 5 });

        var precomputer = new DonorGenotypeSetPrecomputer(
            frequencyLookupService,
            new SubjectGenotypeSetPrecomputeService(
                new SubjectGenotypeSetValueService(failingUpsertRepositoryFactory, genotypeSetService),
                failingUpsertRepositoryFactory),
            Injection.Provider.GetService<IMatchingAlgorithmImportLogger>());

        service = new DonorManagementService(
            repositoryFactory,
            Injection.Provider.GetService<IDonorService>(),
            Injection.Provider.GetService<IMatchingAlgorithmImportLogger>(),
            precomputer);
    }

    [Test]
    public async Task ApplyDonorUpdatesToDatabase_NewDonor_GetsOneRowPerLocusCombination()
    {
        var donor = new DonorInfoBuilder().Build();

        await Import(donor);

        (await AssignmentCount(donor.DonorId)).Should().Be(LocusCombinations);
    }

    [Test]
    public async Task ApplyDonorUpdatesToDatabase_UpdatedDonor_RowsPointAtValuesForItsNewTyping()
    {
        var donorId = DonorIdGenerator.NextId();
        await Import(new DonorInfoBuilder(donorId).Build());
        var oldValueIds = await AssignedValueIds(donorId);

        await Import(new DonorInfoBuilder(donorId).WithHlaAtLocus(Locus.A, LocusPosition.Two, "*02:01").Build());

        var newValueIds = await AssignedValueIds(donorId);
        newValueIds.Should().HaveCount(LocusCombinations);
        newValueIds.Should().NotIntersectWith(oldValueIds);
    }

    [Test]
    public async Task ApplyDonorUpdatesToDatabase_WhenImputationFails_SavesDonorsAndLeavesNewAndUpdatedDonorsWithNoRows()
    {
        var updatedDonorId = DonorIdGenerator.NextId();
        await Import(new DonorInfoBuilder(updatedDonorId).Build());
        (await AssignmentCount(updatedDonorId)).Should().Be(LocusCombinations);

        genotypeSetService.GetGenotypeSet(default, default).ThrowsAsyncForAnyArgs(new Exception("cannot impute"));
        // Both typed differently from the donor imported first, so neither can reuse a value it stored.
        var newDonor = new DonorInfoBuilder().WithHlaAtLocus(Locus.A, LocusPosition.Two, "*02:01").Build();
        var updatedDonor = new DonorInfoBuilder(updatedDonorId).WithHlaAtLocus(Locus.A, LocusPosition.Two, "*02:01").Build();

        var act = () => Import(newDonor, updatedDonor);

        await act.Should().NotThrowAsync();
        await ShouldBeSavedWithNoRows(newDonor, updatedDonor);
    }

    [Test]
    public async Task ApplyDonorUpdatesToDatabase_WhenAssignmentWriteFails_SavesDonorsAndLeavesNewAndUpdatedDonorsWithNoRows()
    {
        var updatedDonorId = DonorIdGenerator.NextId();
        await Import(new DonorInfoBuilder(updatedDonorId).Build());
        (await AssignmentCount(updatedDonorId)).Should().Be(LocusCombinations);

        failingUpsertRepositoryFactory.FailUpserts = true;
        // Both typed differently from the donor imported first, so neither can reuse a value it stored.
        var newDonor = new DonorInfoBuilder().WithHlaAtLocus(Locus.A, LocusPosition.Two, "*02:01").Build();
        var updatedDonor = new DonorInfoBuilder(updatedDonorId).WithHlaAtLocus(Locus.A, LocusPosition.Two, "*02:01").Build();

        var act = () => Import(newDonor, updatedDonor);

        await act.Should().NotThrowAsync();
        await ShouldBeSavedWithNoRows(newDonor, updatedDonor);
    }

    private async Task Import(params DonorInfo[] donors) =>
        await service.ApplyDonorUpdatesToDatabase(
            donors.Select(d => d.ToUpdate()).ToList(),
            TargetDatabase,
            HlaVersion,
            runAllHlaInsertionsInASingleTransactionScope: true);

    private async Task ShouldBeSavedWithNoRows(params DonorInfo[] donors)
    {
        var savedDonors = await repositoryFactory.GetDonorInspectionRepositoryForDatabase(TargetDatabase)
            .GetDonors(donors.Select(d => d.DonorId));

        foreach (var donor in donors)
        {
            savedDonors[donor.DonorId].HlaNames.A.Position2.Should().Be(donor.HlaNames.A.Position2);
            (await AssignmentCount(donor.DonorId)).Should().Be(0);
        }
    }

    private async Task<int> AssignmentCount(int donorId) => (await AssignedValueIds(donorId)).Count;

    private async Task<List<int>> AssignedValueIds(int donorId)
    {
        await using var context = new ContextFactory().Create(targetConnectionString);
        return await context.DonorSubjectGenotypeSets
            .Where(row => row.DonorId == donorId)
            .Select(row => row.SubjectGenotypeSetValueId)
            .ToListAsync();
    }

    /// <summary>
    /// The real factory, except that its genotype set repository can be made to fail the donor assignment write.
    /// </summary>
    private sealed class FailingUpsertRepositoryFactory(IStaticallyChosenDatabaseRepositoryFactory inner) : IStaticallyChosenDatabaseRepositoryFactory
    {
        public bool FailUpserts { get; set; }

        public ISubjectGenotypeSetRepository GetSubjectGenotypeSetRepositoryForDatabase(TransientDatabase targetDatabase) =>
            new FailingUpsertRepository(inner.GetSubjectGenotypeSetRepositoryForDatabase(targetDatabase), () => FailUpserts);

        public IDonorManagementLogRepository GetDonorManagementLogRepositoryForDatabase(TransientDatabase targetDatabase) =>
            inner.GetDonorManagementLogRepositoryForDatabase(targetDatabase);

        public IDonorInspectionRepository GetDonorInspectionRepositoryForDatabase(TransientDatabase targetDatabase) =>
            inner.GetDonorInspectionRepositoryForDatabase(targetDatabase);

        public IDonorUpdateRepository GetDonorUpdateRepositoryForDatabase(TransientDatabase targetDatabase) =>
            inner.GetDonorUpdateRepositoryForDatabase(targetDatabase);

        public IPGroupRepository GetPGroupRepositoryForDatabase(TransientDatabase targetDatabase) =>
            inner.GetPGroupRepositoryForDatabase(targetDatabase);

        public IDonorGenotypePrecomputationRepository GetDonorGenotypePrecomputationRepositoryForDatabase(TransientDatabase targetDatabase) =>
            inner.GetDonorGenotypePrecomputationRepositoryForDatabase(targetDatabase);
    }

    private sealed class FailingUpsertRepository(ISubjectGenotypeSetRepository inner, Func<bool> shouldFail) : ISubjectGenotypeSetRepository
    {
        public Task<IReadOnlyDictionary<SubjectGenotypeSetKey, int>> GetExistingValueIds(IReadOnlyCollection<SubjectGenotypeSetKey> keys) =>
            inner.GetExistingValueIds(keys);

        public Task<IReadOnlyDictionary<SubjectGenotypeSetKey, int>> GetOrCreateValueIds(IReadOnlyCollection<SubjectGenotypeSetValueToStore> values) =>
            inner.GetOrCreateValueIds(values);

        public Task UpsertDonorAssignments(IReadOnlyCollection<DonorSubjectGenotypeSetAssignment> assignments) =>
            shouldFail() ? Task.FromException(new Exception("assignment write failed")) : inner.UpsertDonorAssignments(assignments);

        public Task DeleteDonorAssignments(IReadOnlyCollection<int> donorIds) => inner.DeleteDonorAssignments(donorIds);

        public Task<IReadOnlyDictionary<int, StoredDonorSubjectGenotypeSet>> GetDonorSubjectGenotypeSets(
            IReadOnlyCollection<int> donorIds,
            AllowedLociKey allowedLociKey) =>
            inner.GetDonorSubjectGenotypeSets(donorIds, allowedLociKey);

        public Task<GuardedUpsertResult> UpsertDonorAssignmentsWhereDonorUnchanged(IReadOnlyCollection<GuardedDonorAssignment> assignments) =>
            inner.UpsertDonorAssignmentsWhereDonorUnchanged(assignments);
    }
}
