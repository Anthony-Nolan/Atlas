using System.Collections.Generic;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.Common.Test.SharedTestHelpers;
using Atlas.Common.Test.SharedTestHelpers.Builders;
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

        private static readonly ISet<Locus> AllowedLoci = LocusSettings.MatchPredictionLoci;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            matchCalculationService = DependencyInjection.DependencyInjection.Provider.GetService<IMatchCalculationService>();
        }

        /// <summary>
        /// Note that while in practice this method should be called with P Group typed hla, under the hood the work done is string comparison.
        /// A performance test of this method does not care about biological correctness, so running with G AlleleGroups is sufficient here.
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

        private static PhenotypeInfoBuilder<string> DefaultGGroupsBuilder => new(Alleles.UnambiguousAlleleDetails.GGroups());
    }
}