using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.DonorImport.Data.Repositories;
using Atlas.DonorImport.FileSchema.Models;
using Atlas.DonorImport.Services;
using Atlas.DonorImport.Test.TestHelpers.Builders;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using AwesomeAssertions;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.DonorImport.Test.Services;

[TestFixture]
public class DonorImportLogServiceTests
{
    private static readonly DateTime UploadTime = new(2026, 11, 26, 12, 0, 0, DateTimeKind.Utc);

    private IDonorImportLogRepository repository;
    private IDonorImportLogService donorImportLogService;

    [SetUp]
    public void SetUp()
    {
        repository = Substitute.For<IDonorImportLogRepository>();
        donorImportLogService = new DonorImportLogService(repository);
    }

    [Test]
    public async Task FilterDonorUpdatesBasedOnUpdateTime_WhenFirstDonorHasNewerUpdate_SkipsOnlyThatDonor()
    {
        var updates = DonorUpdateBuilder.New
            .With(u => u.ChangeType, ImportDonorChangeType.Edit)
            .Build(3)
            .ToList();
        SetLastUpdatedTimes(new Dictionary<string, DateTime> { { updates[0].RecordId, UploadTime.AddDays(1) } });

        var result = await Filter(updates);

        result.Should().BeEquivalentTo(new[] { updates[1], updates[2] });
    }

    [Test]
    public async Task FilterDonorUpdatesBasedOnUpdateTime_WhenDonorsHaveNewerUpdates_ReturnsOtherDonorsInOrder()
    {
        var updates = DonorUpdateBuilder.New
            .With(u => u.ChangeType, ImportDonorChangeType.Edit)
            .Build(5)
            .ToList();
        SetLastUpdatedTimes(new Dictionary<string, DateTime>
        {
            { updates[1].RecordId, UploadTime },
            { updates[3].RecordId, UploadTime.AddDays(1) },
        });

        var result = await Filter(updates);

        result.Should().Equal(updates[0], updates[2], updates[4]);
    }

    [Test]
    public async Task FilterDonorUpdatesBasedOnUpdateTime_WhenDonorHasOlderUpdate_ReturnsDonor()
    {
        var updates = DonorUpdateBuilder.New
            .With(u => u.ChangeType, ImportDonorChangeType.Edit)
            .Build(2)
            .ToList();
        SetLastUpdatedTimes(new Dictionary<string, DateTime> { { updates[0].RecordId, UploadTime.AddDays(-1) } });

        var result = await Filter(updates);

        result.Should().Equal(updates);
    }

    private void SetLastUpdatedTimes(IReadOnlyDictionary<string, DateTime> lastUpdatedTimes)
    {
        repository.GetLastUpdatedTimes(Arg.Any<IReadOnlyCollection<string>>()).Returns(lastUpdatedTimes);
    }

    private async Task<List<DonorUpdate>> Filter(IEnumerable<DonorUpdate> updates)
    {
        var result = new List<DonorUpdate>();
        await foreach (var update in donorImportLogService.FilterDonorUpdatesBasedOnUpdateTime(updates, UploadTime))
        {
            result.Add(update);
        }

        return result;
    }
}
