using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.Common.Test.SharedTestHelpers;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.HlaMetadataDictionary.Test.IntegrationTests.TestHelpers.FileBackedStorageStubs;
using Atlas.MatchPrediction.Config;
using Atlas.MatchPrediction.Services.MatchCalculation;
using Atlas.MatchPrediction.Test.Integration.Resources.Alleles;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Integration.IntegrationTests.MatchPrediction.IndividualSteps.MatchCalculation
{
    [TestFixture]
    public class MatchCalculationPerformanceTests
    {
        private IMatchCalculationService matchCalculationService;

        private const string HlaNomenclatureVersion = FileBackedHlaMetadataRepositoryBaseReader.OlderTestHlaVersion;

        private static readonly ISet<Locus> AllowedLoci = LocusSettings.MatchPredictionLoci;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            matchCalculationService = DependencyInjection.DependencyInjection.Provider.GetService<IMatchCalculationService>();
        }
        
        [Test]
        [Repeat(10_000)] // Each test run is 10 method calls.
        [Ignore("This will not be called in production MPA (only by debug endpoints), so not worth running, even on CI. " +
                "This test demonstrates the difference between this approach and the performance enhanced version." +
                "10,000 repeats ~= 6 seconds.")]
        public async Task CalculateMatchCounts_PerformanceTest()
        {
            Task<LociInfo<int?>> CalculateMatchCounts(PhenotypeInfo<string> patient, HashSet<Locus> allowedLoci = null)
            {
                return matchCalculationService.CalculateMatchCounts(
                    patient,
                    DefaultGGroupsBuilder.Build(),
                    HlaNomenclatureVersion,
                    allowedLoci ?? AllowedLoci);
            }

            // Match
            await CalculateMatchCounts(DefaultGGroupsBuilder.Build());

            // Mismatch at A
            const string mismatchHlaA = "01:01:01G";
            await CalculateMatchCounts(DefaultGGroupsBuilder.WithDataAt(Locus.A, mismatchHlaA).Build());
            await CalculateMatchCounts(DefaultGGroupsBuilder.WithDataAt(Locus.A, LocusPosition.One, mismatchHlaA).Build());

            // Mismatch at B
            const string mismatchHlaB  = "38:01:01G";
            await CalculateMatchCounts(DefaultGGroupsBuilder.WithDataAt(Locus.B, mismatchHlaB).Build());
            await CalculateMatchCounts(DefaultGGroupsBuilder.WithDataAt(Locus.B, LocusPosition.Two, mismatchHlaB).Build());

            // Mismatch at C
            const string mismatchHlaC = "14:02:01G";
            await CalculateMatchCounts(DefaultGGroupsBuilder.WithDataAt(Locus.C, mismatchHlaC).Build());

            // Mismatch at DQB1
            const string mismatchHlaDqb1 = "06:09:01G";
            await CalculateMatchCounts(DefaultGGroupsBuilder.WithDataAt(Locus.Dqb1, mismatchHlaDqb1).Build());

            // Mismatch at DRB1
            const string mismatchHlaDrb1 = "14:02:01G";
            await CalculateMatchCounts(DefaultGGroupsBuilder.WithDataAt(Locus.Drb1, mismatchHlaDrb1).Build());
            
            // Mismatch at all
            await CalculateMatchCounts(DefaultGGroupsBuilder
                .WithDataAt(Locus.A, mismatchHlaA)
                .WithDataAt(Locus.B, mismatchHlaB)
                .WithDataAt(Locus.C, mismatchHlaC)
                .WithDataAt(Locus.Dqb1, mismatchHlaDqb1)
                .WithDataAt(Locus.Drb1, mismatchHlaDrb1)
                .Build()
            );
            
            // Mismatch at all, excluded loci
            await CalculateMatchCounts(DefaultGGroupsBuilder
                .WithDataAt(Locus.A, mismatchHlaA)
                .WithDataAt(Locus.B, mismatchHlaB)
                .WithDataAt(Locus.C, mismatchHlaC)
                .WithDataAt(Locus.Dqb1, mismatchHlaDqb1)
                .WithDataAt(Locus.Drb1, mismatchHlaDrb1)
                .Build(),
                new HashSet<Locus> {Locus.C, Locus.Dqb1}
            );
        }
        
        /// <summary>
        /// Note that while in practice this method should be called with P Group typed hla, under the hood the work done is string comparison.
        /// A performance test of this method does not care about biological correctness, so running with G AlleleGroups is sufficient here.
        /// 
        /// As such, we've used the same test data as for <see cref="CalculateMatchCounts_PerformanceTest"/>, to enable reasonable comparison. 
        /// </summary>
        [Test]
        [Repeat(100_000)] // Each test run is 10 method calls.
        [IgnoreExceptOnCiPerfTest("100,000 repeats take ~6 seconds.")]
        public void CalculateMatchCounts_Fast_PerformanceTest()
        {
            void CalculateMatchCounts(PhenotypeInfo<string> patient, HashSet<Locus> allowedLoci = null)
            {
                matchCalculationService.CalculateMatchCounts_Fast(
                    patient,
                    DefaultGGroupsBuilder.Build(),
                    allowedLoci ?? AllowedLoci);
            }

            // Match
            CalculateMatchCounts(DefaultGGroupsBuilder.Build());

            // Mismatch at A
            const string mismatchHlaA = "01:01:01G";
            CalculateMatchCounts(DefaultGGroupsBuilder.WithDataAt(Locus.A, mismatchHlaA).Build());
            CalculateMatchCounts(DefaultGGroupsBuilder.WithDataAt(Locus.A, LocusPosition.One, mismatchHlaA).Build());

            // Mismatch at B
            const string mismatchHlaB  = "38:01:01G";
            CalculateMatchCounts(DefaultGGroupsBuilder.WithDataAt(Locus.B, mismatchHlaB).Build());
            CalculateMatchCounts(DefaultGGroupsBuilder.WithDataAt(Locus.B, LocusPosition.Two, mismatchHlaB).Build());

            // Mismatch at C
            const string mismatchHlaC = "14:02:01G";
            CalculateMatchCounts(DefaultGGroupsBuilder.WithDataAt(Locus.C, mismatchHlaC).Build());

            // Mismatch at DQB1
            const string mismatchHlaDqb1 = "06:09:01G";
            CalculateMatchCounts(DefaultGGroupsBuilder.WithDataAt(Locus.Dqb1, mismatchHlaDqb1).Build());

            // Mismatch at DRB1
            const string mismatchHlaDrb1 = "14:02:01G";
            CalculateMatchCounts(DefaultGGroupsBuilder.WithDataAt(Locus.Drb1, mismatchHlaDrb1).Build());
            
            // Mismatch at all
            CalculateMatchCounts(DefaultGGroupsBuilder
                .WithDataAt(Locus.A, mismatchHlaA)
                .WithDataAt(Locus.B, mismatchHlaB)
                .WithDataAt(Locus.C, mismatchHlaC)
                .WithDataAt(Locus.Dqb1, mismatchHlaDqb1)
                .WithDataAt(Locus.Drb1, mismatchHlaDrb1)
                .Build()
            );
            
            // Mismatch at all, excluded loci
            CalculateMatchCounts(DefaultGGroupsBuilder
                    .WithDataAt(Locus.A, mismatchHlaA)
                    .WithDataAt(Locus.B, mismatchHlaB)
                    .WithDataAt(Locus.C, mismatchHlaC)
                    .WithDataAt(Locus.Dqb1, mismatchHlaDqb1)
                    .WithDataAt(Locus.Drb1, mismatchHlaDrb1)
                    .Build(),
                new HashSet<Locus> {Locus.C, Locus.Dqb1}
            );
        }

        private static PhenotypeInfoBuilder<string> DefaultGGroupsBuilder =>
            new PhenotypeInfoBuilder<string>(Alleles.UnambiguousAlleleDetails.GGroups());
    }
}