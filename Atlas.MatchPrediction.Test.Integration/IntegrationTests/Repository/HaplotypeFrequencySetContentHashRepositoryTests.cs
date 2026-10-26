using System;
using System.Threading.Tasks;
using Atlas.Common.Test.SharedTestHelpers;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.Data.Repositories;
using AutoFixture;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Integration.IntegrationTests.Repository;

/// <summary>The content hashes of the sets, in the real table.</summary>
[TestFixture]
internal class HaplotypeFrequencySetContentHashRepositoryTests
{
    private Fixture fixture;
    private IHaplotypeFrequencySetRepository setRepository;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage(() =>
        {
            setRepository = DependencyInjection.DependencyInjection.Provider.GetService<IHaplotypeFrequencySetRepository>();
        });
    }

    [SetUp]
    public void SetUp()
    {
        fixture = new Fixture();
    }

    [Test]
    public async Task SetContentHash_WritesTheHashToEachSet_AlsoToASetThatHasAHash()
    {
        var setWithNoHash = await AddSet();
        var setWithAHash = await AddSet();
        await setRepository.SetContentHash([setWithAHash.Id], NewHash());
        var hash = NewHash();

        await setRepository.SetContentHash([setWithNoHash.Id, setWithAHash.Id], hash);

        (await setRepository.GetSet(setWithNoHash.Id)).ContentHash.Should().Equal(hash);
        (await setRepository.GetSet(setWithAHash.Id)).ContentHash.Should().Equal(hash);
    }

    [Test]
    public async Task SetContentHashIfMissing_OfASetWithNoHash_WritesTheHash()
    {
        var set = await AddSet();
        var hash = NewHash();

        var written = await setRepository.SetContentHashIfMissing(set.Id, hash);

        written.Should().BeTrue();
        (await setRepository.GetSet(set.Id)).ContentHash.Should().Equal(hash);
    }

    /// <summary>A hash that the import wrote stays: the backfill could have read the set while the import inserted its rows.</summary>
    [Test]
    public async Task SetContentHashIfMissing_OfASetWithAHash_KeepsItsHash()
    {
        var set = await AddSet();
        var hashOfTheImport = NewHash();
        await setRepository.SetContentHash([set.Id], hashOfTheImport);

        var written = await setRepository.SetContentHashIfMissing(set.Id, NewHash());

        written.Should().BeFalse();
        (await setRepository.GetSet(set.Id)).ContentHash.Should().Equal(hashOfTheImport);
    }

    /// <summary>The active sets are the ones in use: their hashes count first.</summary>
    [Test]
    public async Task GetIdsOfSetsWithNoContentHash_ReturnsTheActiveSetsBeforeTheInactiveSets()
    {
        var inactiveSet = await AddSet();
        var activeSet = await AddSet(active: true);

        var setIds = await setRepository.GetIdsOfSetsWithNoContentHash();

        setIds.Should().ContainInOrder(activeSet.Id, inactiveSet.Id);
    }

    [Test]
    public async Task GetIdsOfSetsWithNoContentHash_DoesNotReturnASetWithAHash()
    {
        var set = await AddSet();
        await setRepository.SetContentHash([set.Id], NewHash());

        var setIds = await setRepository.GetIdsOfSetsWithNoContentHash();

        setIds.Should().NotContain(set.Id);
    }

    /// <summary>Other tests add sets to the same table: so this compares the counts before and after its own sets.</summary>
    [Test]
    public async Task GetContentHashCounts_CountsTheSets_TheSetsWithNoHash_AndTheDistinctHashes()
    {
        var countsBefore = await setRepository.GetContentHashCounts();
        var (sharedHash, otherHash) = (NewHash(), NewHash());
        var activeSetA = await AddSet(active: true);
        var activeSetB = await AddSet(active: true);
        var inactiveSet = await AddSet();
        await AddSet();
        await setRepository.SetContentHash([activeSetA.Id, activeSetB.Id], sharedHash);
        await setRepository.SetContentHash([inactiveSet.Id], otherHash);

        var counts = await setRepository.GetContentHashCounts();

        counts.Should().Be(new HaplotypeFrequencySetContentHashCounts(
            countsBefore.Sets + 4,
            countsBefore.SetsWithNoContentHash + 1,
            countsBefore.ContentHashesOfActiveSets + 1,
            countsBefore.ContentHashes + 2));
    }

    /// <summary>A set with codes of its own, so that it replaces no set that another test uses.</summary>
    private async Task<HaplotypeFrequencySet> AddSet(bool active = false)
    {
        var set = await setRepository.AddSet(new HaplotypeFrequencySet
        {
            RegistryCode = fixture.Create<string>(),
            EthnicityCode = fixture.Create<string>(),
            PopulationId = fixture.Create<int>(),
            HlaNomenclatureVersion = fixture.Create<string>(),
            Name = fixture.Create<string>(),
            DateTimeAdded = DateTimeOffset.UtcNow
        });

        if (active)
        {
            await setRepository.ActivateSet(set.Id);
        }

        return set;
    }

    private byte[] NewHash()
    {
        var hash = new byte[32];
        new Random(fixture.Create<int>()).NextBytes(hash);
        return hash;
    }
}
