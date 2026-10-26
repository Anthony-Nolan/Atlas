using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.MatchPrediction.Data.Repositories;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using Atlas.MatchPrediction.Test.Integration.TestHelpers;
using Atlas.MatchPrediction.Test.Integration.TestHelpers.Builders.FrequencySetFile;
using AutoFixture;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Integration.IntegrationTests.Import;

/// <summary>The backfill of the content hashes over the real database, for sets of imports from before content hashes.</summary>
[TestFixture]
internal class FrequencySetContentHashBackfillTests
{
    private Fixture fixture;

    private static IServiceProvider Provider => DependencyInjection.DependencyInjection.Provider;

    [SetUp]
    public void SetUp()
    {
        fixture = new Fixture();
    }

    [Test]
    public async Task Run_WritesTheHashOfTheRowsOfEachSetWithNone_TheSameForTheSetsOfOneImport()
    {
        var setRepository = Provider.GetRequiredService<IHaplotypeFrequencySetRepository>();
        var frequenciesRepository = Provider.GetRequiredService<IHaplotypeFrequenciesRepository>();
        var setIdsOfOneImport = await ImportSets(registryCount: 2, frequency: 0.00001m);
        // Another frequency gives another content.
        var setIdOfAnotherImport = (await ImportSets(registryCount: 1, frequency: 0.00002m)).Single();
        var setIds = setIdsOfOneImport.Append(setIdOfAnotherImport).ToList();
        foreach (var setId in setIds)
        {
            await Provider.GetRequiredService<IHaplotypeFrequencyInspectionRepository>().ClearContentHash(setId);
        }

        var backfill = new FrequencySetContentHashBackfill(setRepository, frequenciesRepository, NullLogger<FrequencySetContentHashBackfill>.Instance);
        await backfill.Run(CancellationToken.None);

        var hashes = new List<byte[]>();
        foreach (var setId in setIds)
        {
            var hash = (await setRepository.GetSet(setId)).ContentHash;
            hash.Should().Equal(FrequencySetContentHash.Calculate(await frequenciesRepository.GetAllHaplotypeFrequencies(setId)));
            hashes.Add(hash);
        }

        hashes[1].Should().Equal(hashes[0]);
        hashes[2].Should().NotEqual(hashes[0]);
        (await backfill.GetCounts()).SetsWithNoContentHash.Should().Be(0);
    }

    /// <returns>The ids of the sets that one import of one haplotype activated, one for each registry.</returns>
    private async Task<List<int>> ImportSets(int registryCount, decimal frequency)
    {
        // Registries of their own, so the other tests' sets do not matter here.
        var registryCodes = fixture.CreateMany<string>(registryCount).ToArray();
        using var file = FrequencySetFileBuilder.New(registryCodes, frequencyValue: frequency).Build();
        await Provider.GetRequiredService<IHaplotypeFrequencyService>().ImportFrequencySet(file);

        var setIds = new List<int>();
        foreach (var registryCode in registryCodes)
        {
            var activeSet = await Provider.GetRequiredService<IHaplotypeFrequencySetRepository>().GetActiveSet(registryCode, null);
            activeSet.Should().NotBeNull("the import must activate the set; a failed import only sends an alert");
            setIds.Add(activeSet.Id);
        }

        return setIds;
    }
}
