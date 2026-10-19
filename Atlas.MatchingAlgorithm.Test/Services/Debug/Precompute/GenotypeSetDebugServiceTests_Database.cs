using System.Threading.Tasks;
using Atlas.Common.Public.Models.MatchPrediction;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.MatchingAlgorithm.Data.Models.Entities;
using Atlas.MatchingAlgorithm.Data.Models.Precompute;
using Atlas.MatchingAlgorithm.Data.Persistent.Models;
using Atlas.MatchingAlgorithm.Services.Debug.Precompute;
using Atlas.MatchingAlgorithm.Test.TestHelpers.Builders.DataRefresh;
using Atlas.MatchPrediction.Models;
using AutoFixture;
using AwesomeAssertions;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Services.Debug.Precompute;

/// <summary>The database that a request reads: <see cref="GenotypeSetDebugRequest.Database"/> (ATL-420).</summary>
[TestFixture]
internal partial class GenotypeSetDebugServiceTests
{
    [Test]
    public async Task Compute_WithTheDormantDatabase_ReadsItsDonorsAndRows_AtTheVersionOfTheLatestRefreshThatWroteToIt()
    {
        var dormantDatabase = activeRecord.Database.Other();
        var dormantRecord = GivenTheLatestRecordThatWroteTo(dormantDatabase);
        var donor = NewDonor();
        GivenDonors(donor);
        var live = GivenTheLiveSet();
        GivenStoredDonorRows((_, allowedLociKey) => PrecomputedRow(donor, allowedLociKey, live));

        var result = await service.Compute(new GenotypeSetDebugRequest
        {
            ExternalDonorCodes = [donor.ExternalDonorCode],
            Database = TransientDatabaseRole.Dormant,
            CompareWithStored = true
        });

        result.Database.Should().Be(dormantDatabase);
        result.HlaNomenclatureVersion.Should().Be(dormantRecord.HlaNomenclatureVersion);
        ParityChecks(result).Should().HaveCount(4).And.OnlyContain(check => check.Result == ParityResult.Match);
        repositoryFactory.Received().GetDonorInspectionRepositoryForDatabase(dormantDatabase);
        repositoryFactory.Received().GetSubjectGenotypeSetRepositoryForDatabase(dormantDatabase);
        repositoryFactory.DidNotReceive().GetDonorInspectionRepositoryForDatabase(activeRecord.Database);
        repositoryFactory.DidNotReceive().GetSubjectGenotypeSetRepositoryForDatabase(activeRecord.Database);
        await genotypeSetService.Received(4).GetGenotypeSet(
            Arg.Any<SubjectData>(),
            Arg.Is<MatchPredictionParameters>(parameters =>
                parameters.MatchingAlgorithmHlaNomenclatureVersion == dormantRecord.HlaNomenclatureVersion));
    }

    [Test]
    public async Task Compute_WithTheDormantDatabase_ForATyping_ComputesAtItsVersion_AndReadsItsRows()
    {
        var dormantDatabase = activeRecord.Database.Other();
        var dormantRecord = GivenTheLatestRecordThatWroteTo(dormantDatabase);

        var result = await service.Compute(new GenotypeSetDebugRequest
        {
            HlaTyping = Typing(),
            AllowedLociKeys = [AllowedLociKey.ABDrb1],
            Database = TransientDatabaseRole.Dormant,
            CompareWithStored = true
        });

        result.Database.Should().Be(dormantDatabase);
        result.HlaNomenclatureVersion.Should().Be(dormantRecord.HlaNomenclatureVersion);
        repositoryFactory.Received().GetSubjectGenotypeSetRepositoryForDatabase(dormantDatabase);
        await genotypeSetRepository.Received(1).GetSubjectGenotypeSetValue(Arg.Any<SubjectGenotypeSetKey>());
    }

    [Test]
    public async Task Compute_WithTheDormantDatabase_WhenNoRefreshHasWrittenToIt_ThrowsUnavailable()
    {
        historyRepository.GetLatestRecordThatWroteToDatabase(default).ReturnsForAnyArgs((DataRefreshRecord)null);

        var act = () => service.Compute(new GenotypeSetDebugRequest { HlaTyping = Typing(), Database = TransientDatabaseRole.Dormant });

        await act.Should().ThrowAsync<GenotypeSetDebugUnavailableException>();
        await genotypeSetService.DidNotReceiveWithAnyArgs().GetGenotypeSet(default, default);
    }

    [Test]
    public async Task Compute_WithTheDormantDatabase_WhenTheRecordHasNoVersion_ThrowsUnavailable()
    {
        historyRepository.GetLatestRecordThatWroteToDatabase(default).ReturnsForAnyArgs(
            DataRefreshRecordBuilder.New.WithDatabase(activeRecord.Database.Other()).Build());

        var act = () => service.Compute(new GenotypeSetDebugRequest { HlaTyping = Typing(), Database = TransientDatabaseRole.Dormant });

        await act.Should().ThrowAsync<GenotypeSetDebugUnavailableException>();
    }

    [Test]
    public async Task Compute_WithTheActiveDatabase_DoesNotReadTheRecordsOfTheDormantDatabase()
    {
        await service.Compute(new GenotypeSetDebugRequest { HlaTyping = Typing(), Database = TransientDatabaseRole.Active });

        await historyRepository.DidNotReceiveWithAnyArgs().GetLatestRecordThatWroteToDatabase(default);
    }

    private DataRefreshRecord GivenTheLatestRecordThatWroteTo(TransientDatabase database)
    {
        var record = DataRefreshRecordBuilder.New
            .WithDatabase(database)
            .With(r => r.Id, fixture.Create<int>())
            .With(r => r.HlaNomenclatureVersion, fixture.Create<string>())
            .Build();
        historyRepository.GetLatestRecordThatWroteToDatabase(database).Returns(record);
        return record;
    }
}
