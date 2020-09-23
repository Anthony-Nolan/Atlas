using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.Common.Test.SharedTestHelpers;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.HlaMetadataDictionary.Test.IntegrationTests.TestHelpers.FileBackedStorageStubs;
using Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using Atlas.MatchPrediction.Services.MatchProbability;
using Atlas.MatchPrediction.Test.Integration.TestHelpers;
using Atlas.MatchPrediction.Test.Integration.TestHelpers.Builders.FrequencySetFile;
using Atlas.MatchPrediction.Test.TestHelpers.Builders.MatchProbabilityInputs;
using LochNessBuilder;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Integration.IntegrationTests.MatchPrediction.Snapshots
{
    [TestFixture]
    internal class SnapshotTestsBase
    {
        protected IMatchProbabilityService MatchProbabilityService { get; set; }

        protected const string HlaNomenclatureVersion = FileBackedHlaMetadataRepositoryBaseReader.OlderTestHlaVersion;


        // Registry and ethnicity values must match those used in test HF set files.
        protected const string Registry1 = "reg-1";
        protected const string Registry2 = "reg-2";
        protected const string Registry3 = "reg-3";
        protected const string Ethnicity1 = "eth-1";
        protected const string Ethnicity2 = "eth-2";

        protected static PhenotypeInfoBuilder<string> DefaultPhenotypeBuilder =>
            new PhenotypeInfoBuilder<string>()
                .WithDataAt(Locus.A, "01:XX", "11:XX")
                .WithDataAt(Locus.B, "27:XX", "35:XX")
                .WithDataAt(Locus.C, "02:XX", "04:XX")
                .WithDataAt(Locus.Dpb1, "03:XX", "04:XX")
                .WithDataAt(Locus.Dqb1, "03:XX", "05:XX")
                .WithDataAt(Locus.Drb1, "11:XX", "16:XX");

        protected static Builder<SingleDonorMatchProbabilityInput> DefaultInputBuilder => SingleDonorMatchProbabilityInputBuilder.Default
            .WithHlaNomenclature(HlaNomenclatureVersion)
            .WithPatientHla(DefaultPhenotypeBuilder.Build())
            .WithDonorHla(DefaultPhenotypeBuilder.Build())
            .WithDonorMetadata(new FrequencySetMetadata())
            .WithPatientMetadata(new FrequencySetMetadata());

        [SetUp]
        protected void SetUp()
        {
            MatchProbabilityService = DependencyInjection.DependencyInjection.Provider.GetService<IMatchProbabilityService>();
        }

        [OneTimeSetUp]
        public async Task OneTimeSetUp()
        {
            await TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage_Async(async () => { await ImportHaplotypeFrequencies(); });
        }

        
        [Test]
        // Confirm that a search with all the default builder values runs successfully
        public async Task Control()
        {
            var matchDetails = await MatchProbabilityService.CalculateMatchProbability(DefaultInputBuilder.Build());

            matchDetails.MatchProbabilities.ShouldHavePercentages(22, 38, 33);
        }

        private static async Task ImportHaplotypeFrequencies()
        {
            var importer = DependencyInjection.DependencyInjection.Provider.GetService<IHaplotypeFrequencyService>();

            var filePaths = new List<string>
            {
                "Atlas.MatchPrediction.Test.Integration.Resources.HaplotypeFrequencySets.global.json",
                "Atlas.MatchPrediction.Test.Integration.Resources.HaplotypeFrequencySets.reg-1-2-eth-1.json",
                "Atlas.MatchPrediction.Test.Integration.Resources.HaplotypeFrequencySets.reg-2-3.json",
                "Atlas.MatchPrediction.Test.Integration.Resources.HaplotypeFrequencySets.reg-2-3-eth-2.json",
            };

            foreach (var filePath in filePaths)
            {
                await using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(filePath))
                using (var file = FrequencySetFileBuilder.FileWithoutContents()
                    .WithHaplotypeFrequencyFileStream(stream)
                    .Build()
                )
                {
                    await importer.ImportFrequencySet(file);
                }
            }
        }
    }
}