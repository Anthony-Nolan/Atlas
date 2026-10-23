using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.MatchPrediction.ApplicationInsights;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.Data.Repositories;
using Atlas.MatchPrediction.ExternalInterface.Settings;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using Atlas.MatchPrediction.Test.Integration.Resources.Alleles;
using Atlas.MatchPrediction.Test.Integration.TestHelpers.Builders.FrequencySetFile;
using Atlas.MatchPrediction.Test.TestHelpers;
using AutoFixture;
using AwesomeAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Integration.IntegrationTests.Import;

/// <summary>
/// The frequency sets of two processes that share one distributed cache, over the real database: the rows that SQL
/// gives, with their decimal(20,20) frequencies, through the value in the distributed cache.
/// </summary>
[TestFixture]
internal class FrequencySetDistributedCacheTests
{
    private Fixture fixture;

    private static IServiceProvider Provider => DependencyInjection.DependencyInjection.Provider;

    [SetUp]
    public void SetUp()
    {
        fixture = new Fixture();
    }

    [Test]
    public async Task GetAllHaplotypeFrequencies_InASecondProcess_UsesTheSetThatTheFirstProcessStored_AndBuildsTheSameEntry()
    {
        var store = DistributedCacheBuilder.NewSharedStore();
        var frequencyRepository = CountingFrequencyRepository();
        var haplotypes = HaplotypesThatShareNames();
        var setId = await ImportSet(haplotypes);

        var entryOfTheFirstProcess = await NewCache(store, frequencyRepository).GetAllHaplotypeFrequencies(setId);
        var entryOfTheSecondProcess = await NewCache(store, frequencyRepository).GetAllHaplotypeFrequencies(setId);

        await frequencyRepository.Received(1).GetAllHaplotypeFrequencies(setId);
        entryOfTheSecondProcess.SetFrequencies.Should().HaveCount(haplotypes.Count);
        entryOfTheSecondProcess.SetFrequencies.Values.Should().OnlyContain(value => value.Frequency.Scale == 20);
        entryOfTheSecondProcess.ShouldBeTheSameEntryAs(entryOfTheFirstProcess);
    }

    /// <summary>
    /// Each combination of the expressed test G groups, so that the rows share names at each locus, as the rows of a real
    /// set do. One frequency for all, as in a tie-break of truncation. The import converts them to P groups, and no two of
    /// these G groups share a P group.
    /// </summary>
    private static List<HaplotypeFrequency> HaplotypesThatShareNames()
    {
        var groups = AlleleGroups.GGroups;

        return (
                from a in Expressed(groups.A)
                from b in Expressed(groups.B)
                from c in Expressed(groups.C)
                from dqb1 in Expressed(groups.Dqb1)
                from drb1 in Expressed(groups.Drb1)
                select new HaplotypeFrequency
                {
                    Hla = new LociInfo<string>(valueA: a, valueB: b, valueC: c, valueDqb1: dqb1, valueDrb1: drb1),
                    Frequency = 0.00001m
                })
            .ToList();

        static IEnumerable<string> Expressed(IEnumerable<string> names) => names.Where(name => !name.EndsWith('N'));
    }

    /// <returns>The id of the set that the import activated.</returns>
    private async Task<int> ImportSet(IEnumerable<HaplotypeFrequency> haplotypes)
    {
        // A registry of its own, so the other tests' sets do not matter here.
        var registryCode = fixture.Create<string>();
        using var file = FrequencySetFileBuilder.New(haplotypes, [registryCode]).Build();
        await Provider.GetRequiredService<IHaplotypeFrequencyService>().ImportFrequencySet(file);

        var activeSet = await Provider.GetRequiredService<IHaplotypeFrequencySetRepository>().GetActiveSet(registryCode, null);
        activeSet.Should().NotBeNull("the import must activate the set; a failed import only sends an alert");
        return activeSet.Id;
    }

    /// <summary>The frequency repository over the real database, with its calls recorded.</summary>
    private static IHaplotypeFrequenciesRepository CountingFrequencyRepository()
    {
        var repository = Provider.GetRequiredService<IHaplotypeFrequenciesRepository>();
        var countingRepository = Substitute.For<IHaplotypeFrequenciesRepository>();
        countingRepository.GetAllHaplotypeFrequencies(default).ReturnsForAnyArgs(call => repository.GetAllHaplotypeFrequencies(call.Arg<int>()));
        return countingRepository;
    }

    /// <summary>The cache of one process: a local cache of its own, over the shared <paramref name="store"/>.</summary>
    private static HaplotypeFrequencyCache NewCache(IDistributedCache store, IHaplotypeFrequenciesRepository frequencyRepository) =>
        new(
            new HaplotypeFrequencySetCacheProvider(AppCacheBuilder.NewCacheOver(new MemoryCache(new MemoryCacheOptions()))),
            DistributedCacheBuilder.NewProviderOver(store),
            Substitute.For<IFrequencySetResidencyTracker>(),
            frequencyRepository,
            Provider.GetRequiredService<IHaplotypeFrequencySetRepository>(),
            Provider.GetRequiredService<IFrequencyConsolidator>(),
            Provider.GetRequiredService<IMatchPredictionLogger<MatchProbabilityLoggingContext>>(),
            Provider.GetRequiredService<IOptions<HaplotypeFrequencySetCacheSettings>>());
}
