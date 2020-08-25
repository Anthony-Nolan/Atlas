using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.MatchPrediction.Test.Verification.Data.Models;
using Atlas.MatchPrediction.Test.Verification.Data.Repositories;
using Atlas.MatchPrediction.Test.Verification.Models;
using Atlas.MatchPrediction.Test.Verification.Services.GenotypeSimulation;
using Atlas.MatchPrediction.Test.Verification.Services.SimulantGeneration;
using NSubstitute;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Verification.Test.UnitTests
{
    [TestFixture]
    public class GenotypeSimulantsGeneratorTests
    {
        private IGenotypeSimulator genotypeSimulator;
        private ISimulantsRepository simulantsRepository;

        private IGenotypeSimulantsGenerator simulantsGenerator;

        [SetUp]
        public void SetUp()
        {
            genotypeSimulator = Substitute.For<IGenotypeSimulator>();
            simulantsRepository = Substitute.For<ISimulantsRepository>();

            simulantsGenerator = new GenotypeSimulantsGenerator(genotypeSimulator, simulantsRepository);
        }

        [Test]
        public async Task GenerateSimulants_SimulatesRequestedNumberOfGenotypes()
        {
            const int genotypeCount = 10;

            await simulantsGenerator.GenerateSimulants(new GenerateSimulantsRequest { SimulantCount = genotypeCount }, default);

            genotypeSimulator.Received().SimulateGenotypes(genotypeCount, Arg.Any<NormalisedHaplotypePool>());
        }

        [TestCase(TestIndividualCategory.Donor)]
        [TestCase(TestIndividualCategory.Patient)]
        public async Task GenerateSimulants_WritesGenotypesToDatabaseWithCorrectMetadata(TestIndividualCategory testIndividualCategory)
        {
            const int testHarnessId = 123;
            genotypeSimulator.SimulateGenotypes(default, default).ReturnsForAnyArgs(new[] { new PhenotypeInfo<string>() });

            await simulantsGenerator.GenerateSimulants(
                new GenerateSimulantsRequest
                {
                    TestHarnessId = testHarnessId,
                    TestIndividualCategory = testIndividualCategory
                },
                default);

            await simulantsRepository.Received().BulkInsertSimulants(Arg.Is<IReadOnlyCollection<Simulant>>(x =>
                x.First().TestHarness_Id == testHarnessId &&
                x.First().TestIndividualCategory == testIndividualCategory &&
                x.First().SimulatedHlaTypingCategory == SimulatedHlaTypingCategory.Genotype &&
                x.First().SourceSimulantId == null
                ));
        }

        [Test]
        public async Task GenerateSimulants_WritesGenotypesToDatabaseWithCorrectlyMappedHla()
        {
            genotypeSimulator.SimulateGenotypes(default, default).ReturnsForAnyArgs(new[]
            {
                new PhenotypeInfoBuilder<string>()
                    .WithDataAt(Locus.A, "a-1", "a-2")
                    .WithDataAt(Locus.B, "b-1", "b-2")
                    .WithDataAt(Locus.C, "c-1", "c-2")
                    .WithDataAt(Locus.Dqb1, "dqb1-1", "dqb1-2")
                    .WithDataAt(Locus.Drb1, "drb1-1", "drb1-2")
                    .Build()
            });

            await simulantsGenerator.GenerateSimulants(new GenerateSimulantsRequest(), default);

            await simulantsRepository.Received().BulkInsertSimulants(Arg.Is<IReadOnlyCollection<Simulant>>(x =>
                x.First().A_1.Equals("a-1") &&
                x.First().A_2.Equals("a-2") &&
                x.First().B_1.Equals("b-1") &&
                x.First().B_2.Equals("b-2") &&
                x.First().C_1.Equals("c-1") &&
                x.First().C_2.Equals("c-2") &&
                x.First().DQB1_1.Equals("dqb1-1") &&
                x.First().DQB1_2.Equals("dqb1-2") &&
                x.First().DRB1_1.Equals("drb1-1") &&
                x.First().DRB1_2.Equals("drb1-2")));
        }
    }
}
