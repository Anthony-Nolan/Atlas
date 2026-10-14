using System;
using System.Threading.Tasks;
using Atlas.Common.Test.SharedTestHelpers;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.Data.Repositories;
using AutoFixture;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using static Atlas.Common.Utils.Extensions.DependencyInjectionUtils;

namespace Atlas.MatchPrediction.Test.Integration.IntegrationTests.Repository;

[TestFixture]
internal class HaplotypeFrequencySetReadRepositoryTests
{
    private Fixture fixture;
    private IHaplotypeFrequencySetRepository setRepository;
    private IHaplotypeFrequencySetReadRepository readRepository;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage(() =>
        {
            var provider = DependencyInjection.DependencyInjection.Provider;
            setRepository = provider.GetService<IHaplotypeFrequencySetRepository>();
            readRepository = new HaplotypeFrequencySetReadRepository(ConnectionStringReader("MatchPredictionSql")(provider));
        });
    }

    [SetUp]
    public void SetUp()
    {
        fixture = new Fixture();
    }

    [Test]
    public async Task GetHaplotypeFrequencySet_ForASetThatANewerSetReplaced_ReturnsIt_AsInactive()
    {
        // Codes of their own, so that these sets replace no set that another test uses.
        var (registryCode, ethnicityCode) = (fixture.Create<string>(), fixture.Create<string>());
        var replaced = await AddActiveSet(registryCode, ethnicityCode);
        await AddActiveSet(registryCode, ethnicityCode);

        var set = await readRepository.GetHaplotypeFrequencySet(replaced.Id);

        set.Should().BeEquivalentTo(new
        {
            replaced.Id,
            replaced.Name,
            replaced.RegistryCode,
            replaced.EthnicityCode,
            replaced.PopulationId,
            replaced.HlaNomenclatureVersion,
            Active = false
        });
    }

    [Test]
    public async Task GetHaplotypeFrequencySet_ForAnActiveSet_ReturnsIt_AsActive()
    {
        var added = await AddActiveSet(fixture.Create<string>(), fixture.Create<string>());

        var set = await readRepository.GetHaplotypeFrequencySet(added.Id);

        set.Active.Should().BeTrue();
    }

    [Test]
    public async Task GetHaplotypeFrequencySet_WhenNoSetHasTheId_ReturnsNull()
    {
        var set = await readRepository.GetHaplotypeFrequencySet(-fixture.Create<int>());

        set.Should().BeNull();
    }

    private async Task<HaplotypeFrequencySet> AddActiveSet(string registryCode, string ethnicityCode)
    {
        var set = await setRepository.AddSet(new HaplotypeFrequencySet
        {
            RegistryCode = registryCode,
            EthnicityCode = ethnicityCode,
            PopulationId = fixture.Create<int>(),
            HlaNomenclatureVersion = fixture.Create<string>(),
            Name = fixture.Create<string>(),
            DateTimeAdded = DateTimeOffset.UtcNow
        });

        await setRepository.ActivateSet(set.Id);
        return set;
    }
}
