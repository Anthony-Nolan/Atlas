using System.Collections.Generic;
using Atlas.Common.ApplicationInsights;
using Atlas.MatchingAlgorithm.Data.Repositories;
using Atlas.MatchingAlgorithm.Data.Repositories.DonorUpdates;
using Atlas.MatchingAlgorithm.Data.Services;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Repositories;

/// <summary>
/// Covers what a bulk write session reports when it closes. A session that reused nothing saved nothing, and there are
/// two supported configurations in which that happens - see DonorUpdateRepositoryBase.OpenBulkWriteSession - so the
/// trace is the only thing that distinguishes "the optimisation ran" from "the optimisation was inert" in a run's logs.
/// </summary>
[TestFixture]
public class DonorUpdateRepositoryBaseTests
{
    private IAtlasLogger logger;
    private IDonorImportRepository repository;

    [SetUp]
    public void SetUp()
    {
        logger = Substitute.For<IAtlasLogger>();
        var connectionStringProvider = Substitute.For<IConnectionStringProvider>();
        connectionStringProvider.GetConnectionString().Returns("Server=not-connected-to;Database=none;");

        repository = new DonorImportRepository(Substitute.For<IHlaNamesRepository>(), connectionStringProvider, logger);
    }

    [Test]
    public void OpenBulkWriteSession_WhenClosedHavingReusedNothing_TracesThatNothingWasReused()
    {
        // No write is made, so nothing is reused. A bulk copy opens no connection until its first write, so this
        // reaches no database despite the connection string being unusable.
        repository.OpenBulkWriteSession().Dispose();

        logger.Received(1).SendTrace(
            Arg.Is<string>(m => m.Contains("without reusing any bulk copy")),
            Arg.Any<LogLevel>(),
            Arg.Is<Dictionary<string, string>>(p => p["ReusedWrites"] == "0"));
    }

    [Test]
    public void OpenBulkWriteSession_WhenDisposedTwice_ReportsOnce()
    {
        var session = repository.OpenBulkWriteSession();

        session.Dispose();
        session.Dispose();

        logger.ReceivedWithAnyArgs(1).SendTrace(default, default, default);
    }
}
