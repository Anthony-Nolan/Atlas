using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.MatchPrediction.Models;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using Atlas.MatchPrediction.Services.HaplotypeFrequencySets;
using Atlas.MatchPrediction.Test.Integration.TestHelpers.Builders.FrequencySetFile;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Integration.IntegrationTests.HaplotypeFrequencySets
{
    [TestFixture]
    public class HaplotypeFrequencySetTests
    {
        private IHaplotypeFrequencySetService service;
        private IFrequencySetService importService;

        public HaplotypeFrequencySetTests()
        {
            service = DependencyInjection.DependencyInjection.Provider.GetService<IHaplotypeFrequencySetService>();
            importService = DependencyInjection.DependencyInjection.Provider.GetService<IFrequencySetService>();
        }

        [SetUp]
        public async Task SetUpData()
        {
            var data = new List<IndividualPopulationData>
            {
                new IndividualPopulationData{ EthnicityId = "01", RegistryId = "02" },
                new IndividualPopulationData{ EthnicityId = "02", RegistryId = "02" },
            };
            
            await ImportAllHaplotypeSets(data);
        }

        [Test]
        public async Task GetHalpotypeSet_GetsTheCorrectSetWhenGivenCorrectInfo()
        {
            var donorInfo = new IndividualPopulationData() {EthnicityId = "01", RegistryId = "02"};
            var patientInfo = new IndividualPopulationData() {EthnicityId = "01", RegistryId = "02"};

            var result = await service.GetHaplotypeFrequencySetId(donorInfo, patientInfo);

            result.Should().Be(donorInfo);
        }

        private async Task ImportAllHaplotypeSets(IEnumerable<IndividualPopulationData> data)
        {
            var tasks = data.Select(set => ImportHaplotypeSet(set.RegistryId, set.EthnicityId));
            await Task.WhenAll(tasks);
        }
        
        private async Task ImportHaplotypeSet(string registry, string ethnicity)
        {
            using var file = FrequencySetFileBuilder.New(registry, ethnicity).Build();
            await importService.ImportFrequencySet(file);
        }
    }
}