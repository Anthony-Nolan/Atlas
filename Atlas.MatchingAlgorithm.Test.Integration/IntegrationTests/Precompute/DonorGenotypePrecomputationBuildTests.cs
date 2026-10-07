using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.MatchingAlgorithm.Client.Models.Donors;
using Atlas.MatchingAlgorithm.Data.Models.Entities;
using Atlas.MatchingAlgorithm.Data.Models.Precompute;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Data.Repositories.Precompute;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.ConnectionStringProviders;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Atlas.MatchingAlgorithm.Test.Integration.TestHelpers;
using AutoFixture;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using BatchStatus = Atlas.MatchingAlgorithm.Data.Models.Entities.DonorGenotypePrecomputationBatchStatus;
using ContextFactory = Atlas.MatchingAlgorithm.Data.Context.ContextFactory;
using Injection = Atlas.MatchingAlgorithm.Test.Integration.DependencyInjection.DependencyInjection;
using RunStatus = Atlas.MatchingAlgorithm.Data.Models.Entities.DonorGenotypePrecomputationRunStatus;
using SearchAlgorithmContext = Atlas.MatchingAlgorithm.Data.Context.SearchAlgorithmContext;

namespace Atlas.MatchingAlgorithm.Test.Integration.IntegrationTests.Precompute;

/// <summary>
/// The start and the build of a precomputation run against a real database: which donors share a group, the order of the
/// group ids, the group → donor rows that the workers read, the batches, the cancel of a run, and the staging check.
/// </summary>
[TestFixture]
public class DonorGenotypePrecomputationBuildTests
{
    /// <summary>The default of the stage.</summary>
    private const int GroupsPerBatch = 1000;

    /// <summary>
    /// The donors between two donors with one typing. A build that grouped donors only within a window of nearby ids
    /// would give the two different groups.
    /// </summary>
    private const int DonorsInBetween = 2000;

    private Fixture fixture;
    private StaticallyChosenTransientSqlConnectionStringProviderFactory connectionStringFactory;
    private IDonorGenotypePrecomputationRepository repository;
    private int nextDonorId;

    [SetUp]
    public void SetUp()
    {
        fixture = new Fixture();
        nextDonorId = 1;

        connectionStringFactory = Injection.Provider.GetService<StaticallyChosenTransientSqlConnectionStringProviderFactory>();
        repository = Injection.Provider.GetService<IStaticallyChosenDatabaseRepositoryFactory>()
            .GetDonorGenotypePrecomputationRepositoryForDatabase(TransientDatabase.DatabaseA);

        DatabaseManager.ClearTransientDatabases();
    }

    #region StartBuild

    [Test]
    public async Task StartBuild_WhenTheRecordHasNoRun_CreatesARunThatIsBuilding()
    {
        var dataRefreshRecordId = fixture.Create<int>();
        var hlaNomenclatureVersion = NewHlaNomenclatureVersion();
        var groupsPerBatch = fixture.Create<int>();

        var run = await repository.StartBuild(dataRefreshRecordId, hlaNomenclatureVersion, groupsPerBatch);

        run.Should().BeEquivalentTo(new
        {
            DataRefreshRecordId = dataRefreshRecordId,
            HlaNomenclatureVersion = hlaNomenclatureVersion,
            Status = RunStatus.Building,
            GroupsPerBatch = groupsPerBatch,
            TotalGroupCount = (int?)null,
            TotalBatchCount = (int?)null,
            TotalDonorAssignmentCount = (int?)null,
            TotalDonorCount = (int?)null,
            ManualRetryCount = 0,
            CompletedUtc = (DateTime?)null
        });
        (await StoredRun(run.Id)).Should().BeEquivalentTo(run);
    }

    [Test]
    public async Task StartBuild_WhenTheRunIsBuilding_KeepsTheRunAndRemovesItsBatchesAndTheStagingData()
    {
        var run = await StartBuild();
        await InsertBatch(run.Id);
        await InsertGroup(run.Id);
        await InsertGroupDonor();

        var restarted = await repository.StartBuild(run.DataRefreshRecordId, run.HlaNomenclatureVersion, run.GroupsPerBatch + 1);

        restarted.Id.Should().Be(run.Id);
        restarted.Status.Should().Be(RunStatus.Building);
        restarted.GroupsPerBatch.Should().Be(run.GroupsPerBatch, "a build that starts again must cut the same batches");
        (await StoredBatches()).Should().BeEmpty();
        (await repository.HasStagingData()).Should().BeFalse();
    }

    [TestCase(RunStatus.Running)]
    [TestCase(RunStatus.Completed)]
    [TestCase(RunStatus.CompletedWithFailures)]
    [TestCase(RunStatus.Cancelled)]
    public async Task StartBuild_WhenTheRunIsNotBuilding_ThrowsAndChangesNothing(RunStatus status)
    {
        var run = await InsertRun(status);
        var batch = await InsertBatch(run.Id);
        await InsertGroup(run.Id);

        var act = () => repository.StartBuild(run.DataRefreshRecordId, run.HlaNomenclatureVersion, run.GroupsPerBatch);

        await act.Should().ThrowAsync<InvalidOperationException>();
        (await StoredRun(run.Id)).Status.Should().Be(status);
        (await StoredBatches()).Should().ContainSingle(stored => stored.Id == batch.Id);
        (await repository.HasStagingData()).Should().BeTrue();
    }

    [TestCase(0)]
    [TestCase(-1)]
    public async Task StartBuild_WithFewerThanOneGroupPerBatch_Throws(int groupsPerBatch)
    {
        var act = () => repository.StartBuild(fixture.Create<int>(), NewHlaNomenclatureVersion(), groupsPerBatch);

        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    #endregion

    #region BuildRun: which donors share a group

    [Test]
    public async Task BuildRun_GivesTwoDonorsWithOneTypingOneGroupAtEachKey_HoweverFarApartTheirIdsAre()
    {
        var first = NewDonor();
        var donorsInBetween = Enumerable.Range(0, DonorsInBetween).Select(_ => NewDonor(first.RegistryCode, first.EthnicityCode));
        var last = CopyOf(first);
        await InsertDonors([first, .. donorsInBetween, last]);

        await Build();

        var groupIds = await StoredGroupIds();
        var groups = await StoredGroups();
        foreach (var key in AllowedLociKeyExtensions.All)
        {
            groupIds[(last.DonorId, key)].Should().Be(groupIds[(first.DonorId, key)], $"the two donors have one typing at {key}");
            groups[groupIds[(first.DonorId, key)]].DonorCount.Should().Be(2);
        }
    }

    [TestCase(Locus.A)]
    [TestCase(Locus.B)]
    [TestCase(Locus.C)]
    [TestCase(Locus.Dpb1)]
    [TestCase(Locus.Dqb1)]
    [TestCase(Locus.Drb1)]
    public async Task BuildRun_GroupsTwoDonorsThatDifferAtOneLocus_OnlyAtTheKeysWithoutThatLocus(Locus locus)
    {
        var donor = NewDonor();
        var other = CopyOf(donor, copy => SetFirstPosition(copy, locus, fixture.Create<string>()));
        await InsertDonors([donor, other]);

        await Build();

        var groupIds = await StoredGroupIds();
        foreach (var key in AllowedLociKeyExtensions.All)
        {
            var shareAGroup = groupIds[(donor.DonorId, key)] == groupIds[(other.DonorId, key)];
            shareAGroup.Should().Be(!key.ToLoci().Contains(locus), $"the donors differ only at {locus}, and the key is {key}");
        }
    }

    [Test]
    public async Task BuildRun_GroupsAMissingPositionWithAnEmptyOne()
    {
        var donor = NewDonor();
        donor.C_1 = null;
        donor.DQB1_2 = null;
        var other = CopyOf(donor, copy =>
        {
            copy.C_1 = string.Empty;
            copy.DQB1_2 = string.Empty;
        });
        await InsertDonors([donor, other]);

        await Build();

        ShareAGroupAtEveryKey(await StoredGroupIds(), donor, other).Should().BeTrue();
    }

    [Test]
    public async Task BuildRun_DoesNotGroupTwoTypingsThatDifferOnlyInLetterCase()
    {
        var donor = NewDonor();
        donor.A_1 = $"a{fixture.Create<string>()}";
        var other = CopyOf(donor, copy => copy.A_1 = donor.A_1.ToUpperInvariant());
        await InsertDonors([donor, other]);

        await Build();

        ShareAGroupAtAnyKey(await StoredGroupIds(), donor, other).Should().BeFalse();
    }

    [Test]
    public async Task BuildRun_DoesNotGroupTwoTypingsThatDifferOnlyInATrailingSpace()
    {
        var donor = NewDonor();
        var other = CopyOf(donor, copy => copy.A_1 = $"{donor.A_1} ");
        await InsertDonors([donor, other]);

        await Build();

        ShareAGroupAtAnyKey(await StoredGroupIds(), donor, other).Should().BeFalse();
    }

    [TestCase(nameof(Donor.RegistryCode))]
    [TestCase(nameof(Donor.EthnicityCode))]
    public async Task BuildRun_WhenOneCodeOfThePairIsDifferent_DoesNotGroupTheDonors(string code)
    {
        var donor = NewDonor();
        var other = CopyOf(donor, copy => SetCode(copy, code, fixture.Create<string>()));
        await InsertDonors([donor, other]);

        await Build();

        ShareAGroupAtAnyKey(await StoredGroupIds(), donor, other).Should().BeFalse();
    }

    [TestCase(nameof(Donor.RegistryCode))]
    [TestCase(nameof(Donor.EthnicityCode))]
    public async Task BuildRun_WhenOneCodeOfThePairDiffersOnlyInATrailingSpace_DoesNotGroupTheDonors(string code)
    {
        var donor = NewDonor();
        var other = CopyOf(donor, copy => SetCode(copy, code, $"{GetCode(donor, code)} "));
        await InsertDonors([donor, other]);

        await Build();

        ShareAGroupAtAnyKey(await StoredGroupIds(), donor, other).Should().BeFalse("the frequency set lookup reads the code as it is");
    }

    [TestCase(nameof(Donor.RegistryCode))]
    [TestCase(nameof(Donor.EthnicityCode))]
    public async Task BuildRun_WhenOneCodeOfThePairDiffersOnlyInLetterCase_DoesNotGroupTheDonors(string code)
    {
        var donor = NewDonor();
        SetCode(donor, code, $"a{fixture.Create<string>()}");
        var other = CopyOf(donor, copy => SetCode(copy, code, GetCode(donor, code).ToUpperInvariant()));
        await InsertDonors([donor, other]);

        await Build();

        ShareAGroupAtAnyKey(await StoredGroupIds(), donor, other).Should().BeFalse("the frequency set lookup reads the code as it is");
    }

    [TestCase(nameof(Donor.RegistryCode))]
    [TestCase(nameof(Donor.EthnicityCode))]
    public async Task BuildRun_WhenOneDonorHasNoCodeAndTheOtherAnEmptyCode_DoesNotGroupTheDonors(string code)
    {
        var donor = NewDonor();
        SetCode(donor, code, null);
        var other = CopyOf(donor, copy => SetCode(copy, code, string.Empty));
        await InsertDonors([donor, other]);

        await Build();

        ShareAGroupAtAnyKey(await StoredGroupIds(), donor, other).Should().BeFalse();
    }

    [Test]
    public async Task BuildRun_GroupsTwoDonorsWithOneTypingAndNoCodes()
    {
        var donor = NewDonor(null, null);
        var other = CopyOf(donor);
        await InsertDonors([donor, other]);

        await Build();

        ShareAGroupAtEveryKey(await StoredGroupIds(), donor, other).Should().BeTrue();
    }

    #endregion

    #region BuildRun: the groups, their donors and the batches

    [Test]
    public async Task BuildRun_PutsEachDonorInExactlyOneGroupOfEachKey()
    {
        var donors = DonorsOfSeveralPairsWithSharedTypings();
        await InsertDonors(donors);

        var result = await Build();

        await using var context = NewContext();
        var keyByGroupId = await context.DonorGenotypePrecomputationGroups.AsNoTracking()
            .ToDictionaryAsync(group => group.Id, group => group.AllowedLociKey);
        var groupDonors = await context.DonorGenotypePrecomputationGroupDonors.AsNoTracking().ToListAsync();

        var keysOfEachDonor = groupDonors
            .GroupBy(groupDonor => groupDonor.DonorId)
            .ToDictionary(rows => rows.Key, rows => rows.Select(row => keyByGroupId[row.GroupId]).Order().ToList());
        keysOfEachDonor.Should().BeEquivalentTo(donors.ToDictionary(donor => donor.DonorId, _ => AllowedLociKeyExtensions.All.Order().ToList()));
        result.TotalDonorAssignmentCount.Should().Be(groupDonors.Count);
    }

    [Test]
    public async Task BuildRun_TakesTheLowestDonorIdAsTheRepresentativeAndCountsTheDonorsOfEachGroup()
    {
        var lowest = NewDonor();
        var middle = CopyOf(lowest);
        var highest = CopyOf(lowest);
        await InsertDonors([highest, lowest, middle]);

        await Build();

        var groups = (await StoredGroups()).Values;
        groups.Should().HaveCount(AllowedLociKeyExtensions.All.Count)
            .And.OnlyContain(group => group.RepresentativeDonorId == lowest.DonorId && group.DonorCount == 3);
    }

    [Test]
    public async Task BuildRun_NumbersTheGroupsFromOneWithNoGap_PairByPairThenKeyByKey()
    {
        var donors = DonorsOfSeveralPairsWithSharedTypings();
        await InsertDonors(donors);

        var result = await Build();

        var groups = (await StoredGroups()).Values.OrderBy(group => group.Id).ToList();
        groups.Select(group => group.Id).Should().Equal(Enumerable.Range(1, result.TotalGroupCount));

        var pairOfDonor = donors.ToDictionary(donor => donor.DonorId, donor => (donor.RegistryCode, donor.EthnicityCode));
        var pairOfEachGroup = groups.Select(group => pairOfDonor[group.RepresentativeDonorId]).ToList();
        var pairChanges = pairOfEachGroup.Where((pair, index) => index > 0 && pair != pairOfEachGroup[index - 1]).Count();
        pairChanges.Should().Be(pairOfDonor.Values.Distinct().Count() - 1, "the groups of each pair are next to each other");

        foreach (var groupsOfAPair in groups.GroupBy(group => pairOfDonor[group.RepresentativeDonorId]))
        {
            groupsOfAPair.Select(group => KeyOrdinal(group.AllowedLociKey)).Should().BeInAscendingOrder("within a pair, the groups run key by key");
        }
    }

    [Test]
    public async Task BuildRun_CutsTheGroupsIntoBatchesOfGroupsPerBatch_InIdOrder()
    {
        // Two donors with nothing in common give two groups at each key. Three groups per batch do not divide them.
        const int groupsPerBatch = 3;
        await InsertDonors([NewDonor(), NewDonor()]);
        var run = await StartBuild(groupsPerBatch);

        var result = await repository.BuildRun(run.Id, CancellationToken.None);

        var expectedBatches = Enumerable.Range(1, result.TotalGroupCount)
            .Chunk(groupsPerBatch)
            .Select((groupIds, batchNumber) => new
            {
                RunId = run.Id,
                BatchNumber = batchNumber,
                FirstGroupId = groupIds[0],
                LastGroupId = groupIds[^1],
                GroupCount = groupIds.Length,
                DonorAssignmentCount = groupIds.Length,
                Status = BatchStatus.Pending,
                RetryCount = 0,
                FailedGroupCount = 0,
                LeaseOwner = (Guid?)null
            })
            .ToList();
        (await StoredBatches()).Should().BeEquivalentTo(expectedBatches);
        result.TotalGroupCount.Should().Be(2 * AllowedLociKeyExtensions.All.Count);
        result.TotalBatchCount.Should().Be(expectedBatches.Count);
    }

    #endregion

    #region BuildRun: the run

    [Test]
    public async Task BuildRun_MovesTheRunToRunningWithTheTotalsOfTheBuild()
    {
        var donors = DonorsOfSeveralPairsWithSharedTypings();
        await InsertDonors(donors);
        var run = await StartBuild();

        var result = await repository.BuildRun(run.Id, CancellationToken.None);

        result.Should().BeEquivalentTo(new
        {
            Status = RunStatus.Running,
            TotalDonorCount = donors.Count,
            TotalGroupCount = (await StoredGroups()).Count,
            TotalBatchCount = (await StoredBatches()).Count,
            TotalDonorAssignmentCount = donors.Count * AllowedLociKeyExtensions.All.Count
        });
        result.Steps.Should().NotBeEmpty();
        (await StoredRun(run.Id)).Should().BeEquivalentTo(new
        {
            Status = RunStatus.Running,
            TotalDonorCount = (int?)result.TotalDonorCount,
            TotalGroupCount = (int?)result.TotalGroupCount,
            TotalBatchCount = (int?)result.TotalBatchCount,
            TotalDonorAssignmentCount = (int?)result.TotalDonorAssignmentCount,
            CompletedUtc = (DateTime?)null
        });
    }

    [Test]
    public async Task BuildRun_WithNoDonors_CompletesTheRunWithNoBatches()
    {
        var run = await StartBuild();

        var result = await repository.BuildRun(run.Id, CancellationToken.None);

        result.Should().BeEquivalentTo(new
        {
            Status = RunStatus.Completed,
            TotalDonorCount = 0,
            TotalGroupCount = 0,
            TotalBatchCount = 0,
            TotalDonorAssignmentCount = 0
        });
        var storedRun = await StoredRun(run.Id);
        storedRun.Status.Should().Be(RunStatus.Completed);
        storedRun.CompletedUtc.Should().NotBeNull();
        (await StoredBatches()).Should().BeEmpty();
    }

    [Test]
    public async Task BuildRun_AfterABuildThatStoppedAndAStartAgain_BuildsFromNothing()
    {
        var donors = DonorsOfSeveralPairsWithSharedTypings();
        await InsertDonors(donors);
        var run = await StartBuild();
        // What an attempt that stopped part way can leave.
        await InsertGroup(run.Id, groupId: 1);
        await InsertGroupDonor(groupId: 1);
        await InsertBatch(run.Id);

        await repository.StartBuild(run.DataRefreshRecordId, run.HlaNomenclatureVersion, run.GroupsPerBatch);
        var result = await repository.BuildRun(run.Id, CancellationToken.None);

        result.TotalDonorAssignmentCount.Should().Be(donors.Count * AllowedLociKeyExtensions.All.Count);
        (await StoredGroupIds()).Should().HaveCount(result.TotalDonorAssignmentCount);
        (await StoredBatches()).Should().HaveCount(result.TotalBatchCount);
    }

    [TestCase(RunStatus.Running)]
    [TestCase(RunStatus.Completed)]
    [TestCase(RunStatus.CompletedWithFailures)]
    [TestCase(RunStatus.Cancelled)]
    public async Task BuildRun_WhenTheRunIsNotBuilding_ThrowsAndWritesNothing(RunStatus status)
    {
        await InsertDonors([NewDonor()]);
        var run = await InsertRun(status);

        var act = () => repository.BuildRun(run.Id, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        (await StoredRun(run.Id)).Status.Should().Be(status);
        (await repository.HasStagingData()).Should().BeFalse();
        (await StoredBatches()).Should().BeEmpty();
    }

    [Test]
    public async Task BuildRun_WhenStagingDataIsLeft_ThrowsAndLeavesTheRunBuilding()
    {
        await InsertDonors([NewDonor()]);
        var run = await StartBuild();
        await InsertGroup(run.Id);

        var act = () => repository.BuildRun(run.Id, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        (await StoredRun(run.Id)).Status.Should().Be(RunStatus.Building);
    }

    [Test]
    public async Task BuildRun_WhenCancelled_ThrowsAndLeavesTheRunBuilding()
    {
        await InsertDonors([NewDonor()]);
        var run = await StartBuild();
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        var cancelledToken = cancellation.Token;

        var act = () => repository.BuildRun(run.Id, cancelledToken);

        await act.Should().ThrowAsync<OperationCanceledException>();
        (await StoredRun(run.Id)).Status.Should().Be(RunStatus.Building);
    }

    #endregion

    #region TryMarkRunCancelled and HasStagingData

    [TestCase(RunStatus.Building)]
    [TestCase(RunStatus.Running)]
    public async Task TryMarkRunCancelled_WhenTheRunIsBuildingOrRunning_CancelsIt(RunStatus status)
    {
        var run = await InsertRun(status);

        var wasCancelled = await repository.TryMarkRunCancelled(run.DataRefreshRecordId);

        wasCancelled.Should().BeTrue();
        (await StoredRun(run.Id)).Status.Should().Be(RunStatus.Cancelled);
    }

    [TestCase(RunStatus.Completed)]
    [TestCase(RunStatus.CompletedWithFailures)]
    [TestCase(RunStatus.Cancelled)]
    public async Task TryMarkRunCancelled_WhenTheRunIsDone_ReturnsFalseAndLeavesIt(RunStatus status)
    {
        var run = await InsertRun(status);

        var wasCancelled = await repository.TryMarkRunCancelled(run.DataRefreshRecordId);

        wasCancelled.Should().BeFalse();
        (await StoredRun(run.Id)).Status.Should().Be(status);
    }

    [Test]
    public async Task TryMarkRunCancelled_WhenTheRecordHasNoRun_ReturnsFalse()
    {
        var run = await InsertRun(RunStatus.Running);

        var wasCancelled = await repository.TryMarkRunCancelled(run.DataRefreshRecordId + 1);

        wasCancelled.Should().BeFalse();
        (await StoredRun(run.Id)).Status.Should().Be(RunStatus.Running);
    }

    [Test]
    public async Task HasStagingData_WhenBothStagingTablesAreEmpty_ReturnsFalse()
    {
        await InsertRun(RunStatus.Running);

        var hasStagingData = await repository.HasStagingData();

        hasStagingData.Should().BeFalse();
    }

    [Test]
    public async Task HasStagingData_WithAGroup_ReturnsTrue()
    {
        await InsertGroup(fixture.Create<int>());

        var hasStagingData = await repository.HasStagingData();

        hasStagingData.Should().BeTrue();
    }

    [Test]
    public async Task HasStagingData_WithAGroupDonor_ReturnsTrue()
    {
        await InsertGroupDonor();

        var hasStagingData = await repository.HasStagingData();

        hasStagingData.Should().BeTrue();
    }

    #endregion

    private async Task<DonorGenotypePrecomputationBuildResult> Build()
    {
        var run = await StartBuild();
        return await repository.BuildRun(run.Id, CancellationToken.None);
    }

    private Task<DonorGenotypePrecomputationRun> StartBuild(int groupsPerBatch = GroupsPerBatch) =>
        repository.StartBuild(fixture.Create<int>(), NewHlaNomenclatureVersion(), groupsPerBatch);

    /// <summary>The column holds 32 characters; a whole AutoFixture string is 36.</summary>
    private string NewHlaNomenclatureVersion() => fixture.Create<string>()[..8];

    /// <summary>
    /// Three pairs. In each pair: donors with typings of their own, a copy of each, and a copy of each that differs at C, so
    /// it shares the groups of the keys without C.
    /// </summary>
    private List<Donor> DonorsOfSeveralPairsWithSharedTypings()
    {
        var donors = new List<Donor>();
        foreach (var _ in Enumerable.Range(0, 3))
        {
            var registryCode = fixture.Create<string>();
            var ethnicityCode = fixture.Create<string>();
            foreach (var donor in Enumerable.Range(0, 4).Select(_ => NewDonor(registryCode, ethnicityCode)).ToList())
            {
                donors.Add(donor);
                donors.Add(CopyOf(donor));
                donors.Add(CopyOf(donor, copy => copy.C_1 = fixture.Create<string>()));
            }
        }

        return donors;
    }

    private Donor NewDonor() => NewDonor(fixture.Create<string>(), fixture.Create<string>());

    private Donor NewDonor(string registryCode, string ethnicityCode) => new()
    {
        DonorId = nextDonorId++,
        DonorType = fixture.Create<DonorType>(),
        IsAvailableForSearch = true,
        ExternalDonorCode = fixture.Create<string>(),
        RegistryCode = registryCode,
        EthnicityCode = ethnicityCode,
        A_1 = fixture.Create<string>(),
        A_2 = fixture.Create<string>(),
        B_1 = fixture.Create<string>(),
        B_2 = fixture.Create<string>(),
        C_1 = fixture.Create<string>(),
        C_2 = fixture.Create<string>(),
        DPB1_1 = fixture.Create<string>(),
        DPB1_2 = fixture.Create<string>(),
        DQB1_1 = fixture.Create<string>(),
        DQB1_2 = fixture.Create<string>(),
        DRB1_1 = fixture.Create<string>(),
        DRB1_2 = fixture.Create<string>()
    };

    /// <summary>A new donor with the codes and the typing of <paramref name="donor"/>, then the change.</summary>
    private Donor CopyOf(Donor donor, Action<Donor> change = null)
    {
        var copy = NewDonor(donor.RegistryCode, donor.EthnicityCode);
        copy.A_1 = donor.A_1;
        copy.A_2 = donor.A_2;
        copy.B_1 = donor.B_1;
        copy.B_2 = donor.B_2;
        copy.C_1 = donor.C_1;
        copy.C_2 = donor.C_2;
        copy.DPB1_1 = donor.DPB1_1;
        copy.DPB1_2 = donor.DPB1_2;
        copy.DQB1_1 = donor.DQB1_1;
        copy.DQB1_2 = donor.DQB1_2;
        copy.DRB1_1 = donor.DRB1_1;
        copy.DRB1_2 = donor.DRB1_2;
        change?.Invoke(copy);
        return copy;
    }

    private static void SetFirstPosition(Donor donor, Locus locus, string value)
    {
        switch (locus)
        {
            case Locus.A:
                donor.A_1 = value;
                break;
            case Locus.B:
                donor.B_1 = value;
                break;
            case Locus.C:
                donor.C_1 = value;
                break;
            case Locus.Dpb1:
                donor.DPB1_1 = value;
                break;
            case Locus.Dqb1:
                donor.DQB1_1 = value;
                break;
            case Locus.Drb1:
                donor.DRB1_1 = value;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(locus), locus, null);
        }
    }

    private static string GetCode(Donor donor, string code) => code switch
    {
        nameof(Donor.RegistryCode) => donor.RegistryCode,
        nameof(Donor.EthnicityCode) => donor.EthnicityCode,
        _ => throw new ArgumentOutOfRangeException(nameof(code), code, null)
    };

    private static void SetCode(Donor donor, string code, string value)
    {
        switch (code)
        {
            case nameof(Donor.RegistryCode):
                donor.RegistryCode = value;
                break;
            case nameof(Donor.EthnicityCode):
                donor.EthnicityCode = value;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(code), code, null);
        }
    }

    private static int KeyOrdinal(AllowedLociKey key) => AllowedLociKeyExtensions.All.ToList().IndexOf(key);

    private static bool ShareAGroupAtEveryKey(Dictionary<(int DonorId, AllowedLociKey Key), int> groupIds, Donor donor, Donor other) =>
        AllowedLociKeyExtensions.All.All(key => groupIds[(donor.DonorId, key)] == groupIds[(other.DonorId, key)]);

    private static bool ShareAGroupAtAnyKey(Dictionary<(int DonorId, AllowedLociKey Key), int> groupIds, Donor donor, Donor other) =>
        AllowedLociKeyExtensions.All.Any(key => groupIds[(donor.DonorId, key)] == groupIds[(other.DonorId, key)]);

    private async Task InsertDonors(IEnumerable<Donor> donors)
    {
        await using var context = NewContext();
        context.Donors.AddRange(donors);
        await context.SaveChangesAsync();
    }

    private async Task<DonorGenotypePrecomputationRun> InsertRun(RunStatus status)
    {
        var run = new DonorGenotypePrecomputationRun
        {
            DataRefreshRecordId = fixture.Create<int>(),
            HlaNomenclatureVersion = NewHlaNomenclatureVersion(),
            Status = status,
            GroupsPerBatch = fixture.Create<int>(),
            CreatedUtc = DateTime.UtcNow,
            StatusDateUtc = DateTime.UtcNow
        };

        await Insert(run);
        return run;
    }

    private async Task<DonorGenotypePrecomputationBatch> InsertBatch(int runId)
    {
        var firstGroupId = fixture.Create<int>();
        var batch = new DonorGenotypePrecomputationBatch
        {
            RunId = runId,
            BatchNumber = fixture.Create<int>(),
            FirstGroupId = firstGroupId,
            LastGroupId = firstGroupId,
            GroupCount = 1,
            DonorAssignmentCount = fixture.Create<int>(),
            Status = BatchStatus.Pending,
            StatusDateUtc = DateTime.UtcNow
        };

        await Insert(batch);
        return batch;
    }

    private Task InsertGroup(int runId, int? groupId = null) =>
        Insert(new DonorGenotypePrecomputationGroup
        {
            Id = groupId ?? fixture.Create<int>(),
            RunId = runId,
            AllowedLociKey = fixture.Create<AllowedLociKey>(),
            RepresentativeDonorId = fixture.Create<int>(),
            DonorCount = fixture.Create<int>()
        });

    private Task InsertGroupDonor(int? groupId = null) =>
        Insert(new DonorGenotypePrecomputationGroupDonor { GroupId = groupId ?? fixture.Create<int>(), DonorId = fixture.Create<int>() });

    /// <summary>The group of each donor at each key.</summary>
    private async Task<Dictionary<(int DonorId, AllowedLociKey Key), int>> StoredGroupIds()
    {
        await using var context = NewContext();
        var keyByGroupId = await context.DonorGenotypePrecomputationGroups.AsNoTracking()
            .ToDictionaryAsync(group => group.Id, group => group.AllowedLociKey);
        var groupDonors = await context.DonorGenotypePrecomputationGroupDonors.AsNoTracking().ToListAsync();

        return groupDonors.ToDictionary(groupDonor => (groupDonor.DonorId, keyByGroupId[groupDonor.GroupId]), groupDonor => groupDonor.GroupId);
    }

    private async Task<Dictionary<int, DonorGenotypePrecomputationGroup>> StoredGroups()
    {
        await using var context = NewContext();
        return await context.DonorGenotypePrecomputationGroups.AsNoTracking().ToDictionaryAsync(group => group.Id);
    }

    private async Task<List<DonorGenotypePrecomputationBatch>> StoredBatches()
    {
        await using var context = NewContext();
        return await context.DonorGenotypePrecomputationBatches.AsNoTracking().ToListAsync();
    }

    private async Task<DonorGenotypePrecomputationRun> StoredRun(int runId)
    {
        await using var context = NewContext();
        return await context.DonorGenotypePrecomputationRuns.AsNoTracking().SingleAsync(run => run.Id == runId);
    }

    /// <summary>A fresh context for each write, so that change tracking cannot hide what the database holds.</summary>
    private async Task Insert<T>(T entity) where T : class
    {
        await using var context = NewContext();
        context.Set<T>().Add(entity);
        await context.SaveChangesAsync();
    }

    private SearchAlgorithmContext NewContext() =>
        new ContextFactory().Create(connectionStringFactory.GenerateConnectionStringProvider(TransientDatabase.DatabaseA).GetConnectionString());
}
