using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Atlas.Common.Test.SharedTestHelpers;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.HlaMetadataDictionary.Test.IntegrationTests.TestHelpers.FileBackedStorageStubs;
using Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using Atlas.MatchPrediction.Services.MatchProbability;
using Atlas.MatchPrediction.Test.Integration.Resources.Alleles;
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
        protected  IMatchProbabilityService MatchProbabilityService { get; set; }
        
        protected const string HlaNomenclatureVersion = FileBackedHlaMetadataRepositoryBaseReader.OlderTestHlaVersion;


        // Registry and ethnicity values must match those used in test HF set files.
        protected const string Registry1 = "pop-16-reg-1";
        protected const string Registry2 = "pop-16-reg-2";
        protected const string Registry3 = "pop-1";
        protected const string Ethnicity1 = "ethnicity-1";
        protected const string Ethnicity2 = "ethnicity-2";

        protected static PhenotypeInfoBuilder<string> DefaultUnambiguousAllelesBuilder =>
            new PhenotypeInfoBuilder<string>(Alleles.UnambiguousAlleleDetails.Alleles());

        protected static PhenotypeInfoBuilder<string> DefaultAmbiguousAllelesBuilder =>
            new PhenotypeInfoBuilder<string>(Alleles.AmbiguousAlleleDetails.Alleles());

        protected static Builder<SingleDonorMatchProbabilityInput> DefaultInputBuilder => SingleDonorMatchProbabilityInputBuilder.Default
            .WithHlaNomenclature(HlaNomenclatureVersion)
            .WithPatientHla(DefaultUnambiguousAllelesBuilder.Build())
            .WithDonorHla(DefaultUnambiguousAllelesBuilder.Build())
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

        private static async Task ImportHaplotypeFrequencies()
        {
            var importer = DependencyInjection.DependencyInjection.Provider.GetService<IHaplotypeFrequencyService>();

            var filePaths = new List<string>
            {
                "Atlas.MatchPrediction.Test.Integration.Resources.HaplotypeFrequencySets.global.json",
                "Atlas.MatchPrediction.Test.Integration.Resources.HaplotypeFrequencySets.pop-1.json",
                "Atlas.MatchPrediction.Test.Integration.Resources.HaplotypeFrequencySets.pop-16.json",
                "Atlas.MatchPrediction.Test.Integration.Resources.HaplotypeFrequencySets.pop-16-ethnicity.json",
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