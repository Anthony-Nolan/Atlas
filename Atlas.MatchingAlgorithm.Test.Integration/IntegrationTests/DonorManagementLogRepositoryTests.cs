using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.MatchingAlgorithm.Data.Models;
using Atlas.MatchingAlgorithm.Data.Repositories;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Atlas.MatchingAlgorithm.Test.Integration.TestHelpers;
using AutoFixture;
using AwesomeAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Injection = Atlas.MatchingAlgorithm.Test.Integration.DependencyInjection.DependencyInjection;

namespace Atlas.MatchingAlgorithm.Test.Integration.IntegrationTests;

public class DonorManagementLogRepositoryTests
{
    private IDonorManagementLogRepository repository;

    private Fixture fixture;

    [SetUp]
    public void SetUp()
    {
        fixture = new Fixture();

        repository = Injection.Provider.GetService<IActiveRepositoryFactory>().GetDonorManagementLogRepository();

        DatabaseManager.ClearTransientDatabases();
    }

    [Test]
    public async Task CreateDonorManagementLogBatch_WhenDonorsHaveNoExistingLogs_CreatesLogPerDonor()
    {
        var infos = fixture.CreateMany<DonorManagementInfo>(3).ToList();

        await repository.CreateDonorManagementLogBatch(infos);

        var logs = (await repository.GetDonorManagementLogBatch(infos.Select(i => i.DonorId))).ToList();
        logs.Select(l => l.DonorId).Should().BeEquivalentTo(infos.Select(i => i.DonorId));
    }

    [Test]
    public async Task CreateDonorManagementLogBatch_WhenNoDonors_DoesNotCreateLogs()
    {
        var act = () => repository.CreateDonorManagementLogBatch(new List<DonorManagementInfo>());

        await act.Should().NotThrowAsync();
    }

    /// <summary>
    /// Documents the precondition of the create-only write: the caller must guarantee the donors have no existing logs.
    /// The data refresh guarantees this by always truncating the log table before importing donors.
    /// </summary>
    [Test]
    public async Task CreateDonorManagementLogBatch_WhenDonorAlreadyHasLog_Throws()
    {
        var info = fixture.Create<DonorManagementInfo>();
        await repository.CreateDonorManagementLogBatch(new[] {info});

        var act = () => repository.CreateDonorManagementLogBatch(new[] {info});

        await act.Should().ThrowAsync<SqlException>();
    }

    [Test]
    public async Task CreateOrUpdateDonorManagementLogBatch_WhenDonorAlreadyHasLog_UpdatesExistingLog()
    {
        var info = fixture.Create<DonorManagementInfo>();
        await repository.CreateDonorManagementLogBatch(new[] {info});

        var updatedInfo = new DonorManagementInfo
        {
            DonorId = info.DonorId,
            UpdateSequenceNumber = info.UpdateSequenceNumber + 1,
            UpdateDateTime = info.UpdateDateTime.AddDays(1)
        };

        await repository.CreateOrUpdateDonorManagementLogBatch(new[] {updatedInfo});

        var logs = (await repository.GetDonorManagementLogBatch(new[] {info.DonorId})).ToList();
        logs.Should().HaveCount(1);
        logs.Single().SequenceNumberOfLastUpdate.Should().Be(updatedInfo.UpdateSequenceNumber);
    }
}
