using System.Threading.Tasks;
using Atlas.MatchPrediction.Data.Repositories;
using Atlas.MatchPrediction.ExternalInterface;
using AutoFixture;
using AwesomeAssertions;
using NSubstitute;
using NUnit.Framework;
using DataHaplotypeFrequencySet = Atlas.MatchPrediction.Data.Models.HaplotypeFrequencySet;

namespace Atlas.MatchPrediction.Test.ExternalInterface;

[TestFixture]
internal class HaplotypeFrequencySetReaderTests
{
    private Fixture fixture;
    private IHaplotypeFrequencySetReadRepository readRepository;
    private IHaplotypeFrequencySetReader reader;

    [SetUp]
    public void SetUp()
    {
        fixture = new Fixture();
        readRepository = Substitute.For<IHaplotypeFrequencySetReadRepository>();
        reader = new HaplotypeFrequencySetReader(readRepository);
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task GetHaplotypeFrequencySet_ReturnsTheSetWithTheId_ActiveOrNot(bool active)
    {
        var stored = fixture.Build<DataHaplotypeFrequencySet>().With(set => set.Active, active).Create();
        readRepository.GetHaplotypeFrequencySet(stored.Id).Returns(stored);

        var set = await reader.GetHaplotypeFrequencySet(stored.Id);

        set.Should().BeEquivalentTo(new
        {
            stored.Id,
            stored.Name,
            stored.RegistryCode,
            stored.EthnicityCode,
            stored.PopulationId,
            stored.HlaNomenclatureVersion
        });
    }

    [Test]
    public async Task GetHaplotypeFrequencySet_WhenNoSetHasTheId_ReturnsNull()
    {
        readRepository.GetHaplotypeFrequencySet(default).ReturnsForAnyArgs((DataHaplotypeFrequencySet)null);

        var set = await reader.GetHaplotypeFrequencySet(fixture.Create<int>());

        set.Should().BeNull();
    }
}
