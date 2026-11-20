using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.ApplicationInsights;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.MultipleAlleleCodeDictionary.AzureStorage.Repositories;
using Atlas.MultipleAlleleCodeDictionary.ExternalInterface;
using Atlas.MultipleAlleleCodeDictionary.ExternalInterface.Models;
using Atlas.MultipleAlleleCodeDictionary.Services;
using Atlas.MultipleAlleleCodeDictionary.Test.TestHelpers.Builders;
using AutoFixture;
using AwesomeAssertions;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.MultipleAlleleCodeDictionary.Test.UnitTests;

/// <summary>
/// The cache service, the store and the expander are real, so that these tests check where each argument goes.
/// Only storage and the logger are substitutes.
/// </summary>
[TestFixture]
internal class MacDictionaryTests
{
    private Fixture fixture;
    private IMacStore macStore;
    private IMacDictionary macDictionary;

    [SetUp]
    public void SetUp()
    {
        fixture = new Fixture();
        macStore = new MacStore();
        var macCacheService = new MacCacheService(Substitute.For<IAtlasLogger>(), macStore, Substitute.For<IMacRepository>(), new MacExpander());
        macDictionary = new MacDictionary(macCacheService);
    }

    [Test]
    public async Task GetHlaFromMac_WithFirstFieldAndMac_ReturnsExpandedAlleles()
    {
        var firstField = fixture.Create<string>();
        var secondFields = fixture.CreateMany<string>(3).ToList();
        var mac = AddGenericMacToStore(secondFields);

        // Named arguments bind to the names of the interface, so this also fails if the class takes them in another order.
        var hla = await macDictionary.GetHlaFromMac(firstField: firstField, mac: mac.Code);

        hla.Should().BeEquivalentTo(secondFields.Select(secondField => $"{firstField}:{secondField}"));
    }

    [Test]
    public async Task GetHlaFromMac_WithMacWithFirstField_ReturnsExpandedAlleles()
    {
        var firstField = fixture.Create<string>();
        var secondFields = fixture.CreateMany<string>(3).ToList();
        var mac = AddGenericMacToStore(secondFields);

        var hla = await macDictionary.GetHlaFromMac($"{firstField}:{mac.Code}");

        hla.Should().BeEquivalentTo(secondFields.Select(secondField => $"{firstField}:{secondField}"));
    }

    /// <summary>
    /// Generic, so that its expansion uses the first field.
    /// </summary>
    private Mac AddGenericMacToStore(IEnumerable<string> secondFields)
    {
        var mac = MacBuilder.New.With(m => m.Hla, string.Join("/", secondFields)).With(m => m.IsGeneric, true).Build();
        macStore.AddMac(mac.Code, new MacValue(mac.Hla, mac.IsGeneric));
        return mac;
    }
}
