using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.MatchPrediction.Config;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.Test.Integration.Resources.Alleles;
using FluentAssertions;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Integration.IntegrationTests.MatchPrediction.MatchProbability
{
    public class ExcludedLociTests : MatchProbabilityTestsBase
    {
        [Test]
        public async Task CalculateMatchProbability_WhenNoExcludedLoci_IncludesAllLociInResults()
        {
            var matchProbabilityInput = DefaultInputBuilder.Build();

            var possibleHaplotypes = new List<HaplotypeFrequency>
            {
                DefaultHaplotypeFrequency1.With(h => h.Frequency, 0.00002m).Build(),
                DefaultHaplotypeFrequency2.With(h => h.Frequency, 0.00001m).Build(),
            };

            await ImportFrequencies(possibleHaplotypes, null, null);

            var matchDetails = await MatchProbabilityService.CalculateMatchProbability(matchProbabilityInput);

            LocusSettings.MatchPredictionLoci.Should()
                .OnlyContain(l => matchDetails.ZeroMismatchProbabilityPerLocus.GetLocus(l) != null, "only excluded loci should be null");
        }

        [TestCase(new[] {Locus.Dqb1})]
        [TestCase(new[] {Locus.C})]
        [TestCase(new[] {Locus.Dqb1, Locus.C})]
        public async Task CalculateMatchProbability_WithExcludedLoci_DoesNotIncludeLociInResult(Locus[] lociToExclude)
        {
            var matchProbabilityInput = DefaultInputBuilder
                .With(h => h.ExcludedLoci, lociToExclude)
                .Build();

            var possibleHaplotypes = new List<HaplotypeFrequency>
            {
                DefaultHaplotypeFrequency1.With(h => h.Frequency, 0.00002m).Build(),
                DefaultHaplotypeFrequency2.With(h => h.Frequency, 0.00001m).Build(),
            };

            await ImportFrequencies(possibleHaplotypes, null, null);

            var matchDetails = await MatchProbabilityService.CalculateMatchProbability(matchProbabilityInput);

            lociToExclude.Should()
                .OnlyContain(l => matchDetails.ZeroMismatchProbabilityPerLocus.GetLocus(l) == null, "excluded loci should be null");
        }
    }
}