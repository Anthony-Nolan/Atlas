using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.Data.Repositories;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using AutoFixture;
using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Services.HaplotypeFrequencies;

[TestFixture]
internal class FrequencySetContentHashBackfillTests
{
    private Fixture fixture;
    private IHaplotypeFrequencySetRepository setRepository;
    private IHaplotypeFrequenciesRepository frequenciesRepository;
    private FrequencySetContentHashBackfill sut;

    [SetUp]
    public void SetUp()
    {
        fixture = new Fixture();
        setRepository = Substitute.For<IHaplotypeFrequencySetRepository>();
        setRepository.GetContentHashCounts().Returns(fixture.Create<HaplotypeFrequencySetContentHashCounts>());
        frequenciesRepository = Substitute.For<IHaplotypeFrequenciesRepository>();

        sut = new FrequencySetContentHashBackfill(setRepository, frequenciesRepository, NullLogger<FrequencySetContentHashBackfill>.Instance);
    }

    /// <summary>
    /// Only where the set still has no hash: an import of the set, which can run at the same time, writes the hash of all
    /// its rows.
    /// </summary>
    [Test]
    public async Task Run_WritesTheHashOfTheRowsOfEachSetWithNoHash_OnlyIfTheSetStillHasNone()
    {
        var rowsBySet = fixture.CreateMany<int>(3).ToDictionary(setId => setId, _ => fixture.CreateMany<LightweightHaplotypeFrequencyRecord>().ToList());
        setRepository.GetIdsOfSetsWithNoContentHash().Returns(rowsBySet.Keys.ToList());
        foreach (var (setId, rows) in rowsBySet)
        {
            frequenciesRepository.GetAllHaplotypeFrequencies(setId).Returns(rows);
        }

        var setsHashed = await sut.Run(CancellationToken.None);

        setsHashed.Should().Be(rowsBySet.Count);
        foreach (var (setId, rows) in rowsBySet)
        {
            var hash = FrequencySetContentHash.Calculate(rows);
            await setRepository.Received(1).SetContentHashIfMissing(setId, Arg.Is<byte[]>(written => written.SequenceEqual(hash)));
        }

        await setRepository.DidNotReceiveWithAnyArgs().SetContentHash(default, default);
    }

    [Test]
    public async Task Run_HashesTheSetsInTheOrderOfTheRepository()
    {
        var setIds = fixture.CreateMany<int>(3).ToList();
        setRepository.GetIdsOfSetsWithNoContentHash().Returns(setIds);
        frequenciesRepository.GetAllHaplotypeFrequencies(default).ReturnsForAnyArgs(new List<LightweightHaplotypeFrequencyRecord>());

        await sut.Run(CancellationToken.None);

        Received.InOrder(() =>
        {
            foreach (var setId in setIds)
            {
                frequenciesRepository.GetAllHaplotypeFrequencies(setId);
            }
        });
    }

    /// <summary>Between two sets: a later run hashes the sets that are left.</summary>
    [Test]
    public async Task Run_WhenCancelled_FinishesTheSetThatItReads_AndStartsNoOtherSet()
    {
        var setIds = fixture.CreateMany<int>(3).ToList();
        setRepository.GetIdsOfSetsWithNoContentHash().Returns(setIds);
        using var cancellation = new CancellationTokenSource();
        frequenciesRepository.GetAllHaplotypeFrequencies(default).ReturnsForAnyArgs(_ =>
        {
            cancellation.Cancel();
            return Task.FromResult(fixture.CreateMany<LightweightHaplotypeFrequencyRecord>().ToList());
        });

        var setsHashed = await sut.Run(cancellation.Token);

        setsHashed.Should().Be(1);
        await frequenciesRepository.ReceivedWithAnyArgs(1).GetAllHaplotypeFrequencies(default);
        await setRepository.Received(1).SetContentHashIfMissing(setIds[0], Arg.Any<byte[]>());
    }

    [Test]
    public async Task GetCounts_ReturnsTheCountsOfTheRepository()
    {
        var counts = fixture.Create<HaplotypeFrequencySetContentHashCounts>();
        setRepository.GetContentHashCounts().Returns(counts);

        var result = await sut.GetCounts();

        result.Should().Be(counts);
    }
}
