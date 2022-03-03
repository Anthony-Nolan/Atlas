using System.Reflection;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.Common.Test.SharedTestHelpers;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.MatchPrediction.ExternalInterface.Models.HaplotypeFrequencySet;
using Atlas.MatchPrediction.ExternalInterface.Models.MatchProbability;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies.Import;
using Atlas.MatchPrediction.Test.Integration.TestHelpers;
using Atlas.MatchPrediction.Test.Integration.TestHelpers.Builders.FrequencySetFile;
using Atlas.MatchPrediction.Test.TestHelpers.Builders.MatchProbabilityInputs;
using FluentAssertions;
using LochNessBuilder;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Integration.IntegrationTests.MatchPrediction.MatchProbability
{
    /// <summary>
    /// Note that all timing information logged for these tests is considering these tests run as part of the whole suite.
    /// Running an individual test alone will add a few seconds, due to one-off suite startup time. (e.g. populating in memory metadata dictionary).  
    /// </summary>
    [TestFixture]
    internal class PerformanceBenchmarks : MatchProbabilityTestsBase
    {
        private const string EthnicityCode = "performance-benchmark-ethnicity";
        private const string RegistryCode = "performance-benchmark-registry";

        private readonly FrequencySetMetadata frequencySetMetadata = new FrequencySetMetadata
        {
            EthnicityCode = EthnicityCode, 
            RegistryCode = RegistryCode
        };

        private Builder<SingleDonorMatchProbabilityInput> InputBuilder => DefaultInputBuilder
            .WithDonorMetadata(frequencySetMetadata)
            .WithPatientMetadata(frequencySetMetadata);

        // Selected to match common frequency in large haplotype frequency set. 
        private static PhenotypeInfoBuilder<string> UnambiguousPhenotypeBuilder => new PhenotypeInfoBuilder<string>()
            .WithDataAt(Locus.A, "01:01:01")
            .WithDataAt(Locus.B, "08:01:01")
            .WithDataAt(Locus.C, "07:01:01")
            .WithDataAt(Locus.Dqb1, "02:01:01")
            .WithDataAt(Locus.Drb1, "03:01:01");

        [OneTimeSetUp]
        public async Task OneTimeSetUp()
        {
            await TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage_Async(async () => { await ImportHaplotypeFrequencies(); });
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage(DatabaseManager.ClearDatabases);
        }

        [Test]
        // Runs in ~0.1 seconds. Quick enough to not ignore.
        public async Task MatchPrediction__WithSmallAmbiguityAtEachDonorLocus__CalculatesProbabilityCorrectly()
        {
            var donorHla = new PhenotypeInfoBuilder<string>()
                .WithDataAt(Locus.A, "01:01:01/02:01:01")
                .WithDataAt(Locus.B, "08:01:01/07:02:01")
                .WithDataAt(Locus.C, "07:01:01/02:02:02")
                .WithDataAt(Locus.Dqb1, "02:01:01/06:02:01")
                .WithDataAt(Locus.Drb1, "03:01:01/07:01:01")
                .Build();

            var patientHla = UnambiguousPhenotypeBuilder.Build();

            var matchProbabilityInput = InputBuilder.WithDonorHla(donorHla).WithPatientHla(patientHla).Build();

            var matchDetails = await MatchProbabilityService.CalculateMatchProbability(matchProbabilityInput);

            matchDetails.MatchProbabilities.ShouldHavePercentages(74,23,2);
        }

        [Test]
        // Runs in ~0.1s. Quick enough to not ignore.
        public async Task MatchPrediction__WithDonorFullyTyped_AtTruncatedTwoFieldAlleleResolution__CalculatesProbabilityCorrectly()
        {
            var donorHla = new PhenotypeInfoBuilder<string>()
                .WithDataAt(Locus.A, "01:01")
                .WithDataAt(Locus.B, "08:01")
                .WithDataAt(Locus.C, "07:01")
                .WithDataAt(Locus.Dqb1, "02:01")
                .WithDataAt(Locus.Drb1, "03:01")
                .Build();

            var patientHla = UnambiguousPhenotypeBuilder.Build();

            var matchProbabilityInput = InputBuilder.WithDonorHla(donorHla).WithPatientHla(patientHla).Build();

            var matchDetails = await MatchProbabilityService.CalculateMatchProbability(matchProbabilityInput);

            matchDetails.MatchProbabilities.ShouldHavePercentages(100,0,0);
        }

        [Test]
        // Runs in ~0.1s. Quick enough to not ignore.
        public async Task MatchPrediction__WithDonorFullyTyped_AtXXCodeResolution__CalculatesProbabilityCorrectly()
        {
            var donorHla = new PhenotypeInfoBuilder<string>()
                .WithDataAt(Locus.A, "01:XX")
                .WithDataAt(Locus.B, "08:XX")
                .WithDataAt(Locus.C, "07:XX")
                .WithDataAt(Locus.Dqb1, "02:XX")
                .WithDataAt(Locus.Drb1, "03:XX")
                .Build();

            var patientHla = UnambiguousPhenotypeBuilder.Build();

            var matchProbabilityInput = InputBuilder.WithDonorHla(donorHla).WithPatientHla(patientHla).Build();

            var matchDetails = await MatchProbabilityService.CalculateMatchProbability(matchProbabilityInput);

            matchDetails.MatchProbabilities.ShouldHavePercentages(99,1,0);
        }

        [Test]
        [IgnoreExceptOnCiPerfTest("Takes around 7 seconds")]
        public async Task MatchPrediction__WithDonor_AndPatient_FullyTypedAtXXCodeResolution__CalculatesProbabilityCorrectly()
        {
            var xxTypedHla = new PhenotypeInfoBuilder<string>()
                .WithDataAt(Locus.A, "01:XX")
                .WithDataAt(Locus.B, "08:XX")
                .WithDataAt(Locus.C, "07:XX")
                .WithDataAt(Locus.Dqb1, "02:XX")
                .WithDataAt(Locus.Drb1, "03:XX")
                .Build();

            var matchProbabilityInput = InputBuilder
                .WithDonorHla(xxTypedHla)
                .WithPatientHla(xxTypedHla)
                .Build();

            var matchDetails = await MatchProbabilityService.CalculateMatchProbability(matchProbabilityInput);

            matchDetails.MatchProbabilities.ShouldHavePercentages(97,3,0);
        }

        [Test]
        [IgnoreExceptOnCiPerfTest("Takes around 30 seconds")]
        public async Task MatchPrediction__WithDonor_AndPatient_FullyTypedAtXXCodeResolution_DifficultHla_1__CalculatesProbabilityCorrectly()
        {
            var xxTypedHla = new PhenotypeInfoBuilder<string>()
                .WithDataAt(Locus.A, LocusPosition.One, "02:XX")
                .WithDataAt(Locus.A, LocusPosition.Two, "24:XX")
                .WithDataAt(Locus.B, LocusPosition.One, "15:XX")
                .WithDataAt(Locus.B, LocusPosition.Two, "40:XX")
                .WithDataAt(Locus.Drb1, LocusPosition.One, "08:XX")
                .WithDataAt(Locus.Drb1, LocusPosition.Two, "11:XX")
                .Build();

            var matchProbabilityInput = InputBuilder
                .WithDonorHla(xxTypedHla)
                .WithPatientHla(xxTypedHla)
                .Build();

            var matchDetails = await MatchProbabilityService.CalculateMatchProbability(matchProbabilityInput);

            matchDetails.MatchProbabilities.ShouldHavePercentages(2,5,11);
        }

        [Test]
        [IgnoreExceptOnCiPerfTest("Takes around 45 seconds")]
        public async Task MatchPrediction__WithDonor_AndPatient_FullyTypedAtXXCodeResolution_DifficultHla_2__CalculatesProbabilityCorrectly()
        {
            var xxTypedHla = new PhenotypeInfoBuilder<string>()
                .WithDataAt(Locus.A, LocusPosition.One, "02:XX")
                .WithDataAt(Locus.A, LocusPosition.Two, "24:XX")
                .WithDataAt(Locus.B, LocusPosition.One, "15:XX")
                .WithDataAt(Locus.B, LocusPosition.Two, "40:XX")
                .WithDataAt(Locus.Drb1, LocusPosition.One, "04:XX")
                .WithDataAt(Locus.Drb1, LocusPosition.Two, "13:XX")
                .Build();

            var matchProbabilityInput = InputBuilder
                .WithDonorHla(xxTypedHla)
                .WithPatientHla(xxTypedHla)
                .Build();

            var matchDetails = await MatchProbabilityService.CalculateMatchProbability(matchProbabilityInput);

            matchDetails.MatchProbabilities.ShouldHavePercentages(3,7,14);
        }

        /// <summary>
        /// Calculated by using XX codes for 2x most common first fields among required loci, as of 03/03/22 
        /// </summary>
        [Test]
        [IgnoreExceptOnCiPerfTest("Takes around 40 seconds")]
        public async Task MatchPrediction__WithDonor_AndPatient_FullyTypedAtXXCodeResolution_DifficultHla_3__CalculatesProbabilityCorrectly()
        {
            var xxTypedHla = new PhenotypeInfoBuilder<string>()
                .WithDataAt(Locus.A, LocusPosition.One, "02:XX")
                .WithDataAt(Locus.A, LocusPosition.Two, "24:XX")
                .WithDataAt(Locus.B, LocusPosition.One, "15:XX")
                .WithDataAt(Locus.B, LocusPosition.Two, "35:XX")
                .WithDataAt(Locus.Drb1, LocusPosition.One, "04:XX")
                .WithDataAt(Locus.Drb1, LocusPosition.Two, "13:XX")
                .Build();

            var matchProbabilityInput = InputBuilder
                .WithDonorHla(xxTypedHla)
                .WithPatientHla(xxTypedHla)
                .Build();

            var matchDetails = await MatchProbabilityService.CalculateMatchProbability(matchProbabilityInput);

            matchDetails.MatchProbabilities.ShouldHavePercentages(1,4,13);
        }

        /// <summary>
        /// Homozygous cases of most common first field among required loci, as of 03/03/22
        /// Ends up expanding to significantly less options than heterozygous cases, so this isn't all that bad as worst cases go.
        /// </summary>
        [Test]
        [IgnoreExceptOnCiPerfTest("Takes around 13 seconds")]
        public async Task MatchPrediction__WithDonor_AndPatient_FullyTypedAtXXCodeResolution_DifficultHla_4__CalculatesProbabilityCorrectly()
        {
            var xxTypedHla = new PhenotypeInfoBuilder<string>()
                .WithDataAt(Locus.A, LocusPosition.One, "02:XX")
                .WithDataAt(Locus.A, LocusPosition.Two, "02:XX")
                .WithDataAt(Locus.B, LocusPosition.One, "35:XX")
                .WithDataAt(Locus.B, LocusPosition.Two, "35:XX")
                .WithDataAt(Locus.Drb1, LocusPosition.One, "04:XX")
                .WithDataAt(Locus.Drb1, LocusPosition.Two, "04:XX")
                .Build();

            var matchProbabilityInput = InputBuilder
                .WithDonorHla(xxTypedHla)
                .WithPatientHla(xxTypedHla)
                .Build();

            var matchDetails = await MatchProbabilityService.CalculateMatchProbability(matchProbabilityInput);

            matchDetails.MatchProbabilities.ShouldHavePercentages(0,2,10);
        }

        [Test]
        // Runs in ~2s. Quick enough to not ignore.
        public async Task MatchPrediction__WithDonor_TypedAtXXCodeResolution_AtRequiredLociOnly__CalculatesProbabilityCorrectly()
        {
            var donorHla = new PhenotypeInfoBuilder<string>((string) null)
                .WithDataAt(Locus.A, "01:XX")
                .WithDataAt(Locus.B, "08:XX")
                .WithDataAt(Locus.Drb1, "03:XX")
                .Build();

            var patientHla = UnambiguousPhenotypeBuilder.Build();

            var matchProbabilityInput = InputBuilder.WithDonorHla(donorHla).WithPatientHla(patientHla).Build();

            var matchDetails = await MatchProbabilityService.CalculateMatchProbability(matchProbabilityInput);

            matchDetails.MatchProbabilities.ShouldHavePercentages(96,4,0);
        }
        
        [Test]
        // During testing in a WMDA sized test dataset in Feb-22, this was the slowest logged patient/donor pair calculation 
        [IgnoreExceptOnCiPerfTest("Takes around 10 seconds")]
        public async Task MatchPrediction__SlowestObservedWMDACase()
        {
            var matchProbabilityInput = InputBuilder
                .WithDonorHla(new PhenotypeInfoBuilder<string>()
                    .WithDataAt(Locus.A, LocusPosition.One, "02:XX")
                    .WithDataAt(Locus.A, LocusPosition.Two, "11:XX")
                    .WithDataAt(Locus.B, LocusPosition.One, "35:XX")
                    .WithDataAt(Locus.B, LocusPosition.Two, "40:XX")
                    .WithDataAt(Locus.Drb1, LocusPosition.One, "11:XX")
                    .WithDataAt(Locus.Drb1, LocusPosition.Two, "15:XX")
                    .Build())
                .WithPatientHla(new PhenotypeInfoBuilder<string>()
                    .WithDataAt(Locus.A, LocusPosition.One, "02:XX")
                    .WithDataAt(Locus.A, LocusPosition.Two, "11:XX")
                    .WithDataAt(Locus.B, LocusPosition.One, "35:XX")
                    .WithDataAt(Locus.B, LocusPosition.Two, "40:XX")
                    .WithDataAt(Locus.C, LocusPosition.One, "04:XX")
                    .WithDataAt(Locus.C, LocusPosition.Two, "15:XX")
                    .WithDataAt(Locus.Dpb1, LocusPosition.One, "04:01")
                    .WithDataAt(Locus.Dpb1, LocusPosition.Two, "04:01")
                    .WithDataAt(Locus.Dqb1, LocusPosition.One, "03:XX")
                    .WithDataAt(Locus.Dqb1, LocusPosition.Two, "06:XX")
                    .WithDataAt(Locus.Drb1, LocusPosition.One, "11:XX")
                    .WithDataAt(Locus.Drb1, LocusPosition.Two, "15:XX")
                    .Build())
                .Build();

            var matchDetails = await MatchProbabilityService.CalculateMatchProbability(matchProbabilityInput);

            matchDetails.MatchProbabilities.ShouldHavePercentages(1,4,8);
        }

        [Test]
        public async Task
            MatchPrediction__WithDonor_TypedAtXXCodeResolution_AtRequiredLociOnly_AndPatient_UnambiguouslyTyped_AtRequiredLociOnly__CalculatesProbabilityCorrectly()
        {
            var donorHla = new PhenotypeInfoBuilder<string>((string) null)
                .WithDataAt(Locus.A, "01:XX")
                .WithDataAt(Locus.B, "08:XX")
                .WithDataAt(Locus.Drb1, "03:XX")
                .Build();

            var patientHla = UnambiguousPhenotypeBuilder
                .WithDataAtLoci(null, Locus.C, Locus.Dqb1)
                .Build();

            var matchProbabilityInput = InputBuilder
                .WithDonorHla(donorHla)
                .WithPatientHla(patientHla)
                .Build();

            var matchDetails = await MatchProbabilityService.CalculateMatchProbability(matchProbabilityInput);

            matchDetails.MatchProbabilities.ShouldHavePercentages(93,7,0);
        }

        private static async Task ImportHaplotypeFrequencies()
        {
            var importer = DependencyInjection.DependencyInjection.Provider.GetService<IHaplotypeFrequencyService>();

            // This file is an actual test file, representing frequencies from one of the largest registries. Pop30 = NMDP.
            var filePath = $"Atlas.MatchPrediction.Test.Integration.Resources.HaplotypeFrequencySets.large.json";

            await using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(filePath))
            using (var file = FrequencySetFileBuilder.FileWithoutContents()
                .WithHaplotypeFrequencyFileStream(stream)
                .Build()
            )
            {
                await importer.ImportFrequencySet(file, new FrequencySetImportBehaviour{ ShouldBypassHlaValidation = true});
            }
        }
    }
}