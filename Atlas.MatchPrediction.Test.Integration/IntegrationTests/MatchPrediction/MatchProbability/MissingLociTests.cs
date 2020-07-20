using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.MatchPrediction.Config;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.Test.Integration.Resources;
using FluentAssertions;
using LochNessBuilder;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Integration.IntegrationTests.MatchPrediction.MatchProbability
{
    public class MissingLociTests : MatchProbabilityTestsBase
    {
        [Test]
        public async Task CalculateMatchProbability_WhenNoNullLoci_IncludesAllLociInResults()
        {
            var matchProbabilityInput = DefaultInputBuilder.Build();

            await ImportFrequencies(new List<HaplotypeFrequency> { Builder<HaplotypeFrequency>.New.Build() });

            var matchDetails = await MatchProbabilityService.CalculateMatchProbability(matchProbabilityInput);

            LocusSettings.MatchPredictionLoci.Should()
                .OnlyContain(l => matchDetails.ZeroMismatchProbabilityPerLocus.GetLocus(l) != null, "only excluded loci should be null");
        }

        [TestCase(new[] {Locus.Dqb1})]
        [TestCase(new[] {Locus.C})]
        [TestCase(new[] {Locus.Dqb1, Locus.C})]
        public async Task CalculateMatchProbability_WhenPatientHlaHasNullLoci_DoesNotIncludeLociInResult(Locus[] lociToExclude)
        {
            var matchProbabilityInput = DefaultInputBuilder
                .With(h => h.PatientHla,
                    new PhenotypeInfoBuilder<string>(Alleles.UnambiguousAlleleDetails.Alleles()).WithDataAtLoci(null, lociToExclude).Build())
                .Build();

            await ImportFrequencies(new List<HaplotypeFrequency> {Builder<HaplotypeFrequency>.New.Build()});

            var matchDetails = await MatchProbabilityService.CalculateMatchProbability(matchProbabilityInput);

            lociToExclude.Should()
                .OnlyContain(l => matchDetails.ZeroMismatchProbabilityPerLocus.GetLocus(l) == null, "excluded loci should be null");
        }

        [TestCase(new[] {Locus.Dqb1})]
        [TestCase(new[] {Locus.C})]
        [TestCase(new[] {Locus.Dqb1, Locus.C})]
        public async Task CalculateMatchProbability_WhenDonorHlaHasNullLoci_DoesNotIncludeLociInResult(Locus[] lociToExclude)
        {
            var matchProbabilityInput = DefaultInputBuilder
                .With(h => h.DonorHla,
                    new PhenotypeInfoBuilder<string>(Alleles.UnambiguousAlleleDetails.Alleles()).WithDataAtLoci(null, lociToExclude).Build())
                .Build();

            await ImportFrequencies(new List<HaplotypeFrequency> { Builder<HaplotypeFrequency>.New.Build() });

            var matchDetails = await MatchProbabilityService.CalculateMatchProbability(matchProbabilityInput);

            lociToExclude.Should()
                .OnlyContain(l => matchDetails.ZeroMismatchProbabilityPerLocus.GetLocus(l) == null, "excluded loci should be null");
        }

        [TestCase(new[] {Locus.Dqb1}, new[] {Locus.C})]
        [TestCase(new[] {Locus.C}, new[] {Locus.C})]
        [TestCase(new[] {Locus.Dqb1, Locus.C}, new[] {Locus.C})]
        public async Task CalculateMatchProbability_WhenPatientAndDonorHlaHasNullLoci_DoesNotIncludeLociInResult(
            Locus[] donorLociToExclude,
            Locus[] patientLociToExclude)
        {
            var matchProbabilityInput = DefaultInputBuilder
                .With(h => h.DonorHla,
                    new PhenotypeInfoBuilder<string>(Alleles.UnambiguousAlleleDetails.Alleles()).WithDataAtLoci(null, donorLociToExclude).Build())
                .With(h => h.PatientHla,
                    new PhenotypeInfoBuilder<string>(Alleles.UnambiguousAlleleDetails.Alleles()).WithDataAtLoci(null, patientLociToExclude).Build())
                .Build();

            await ImportFrequencies(new List<HaplotypeFrequency> {Builder<HaplotypeFrequency>.New.Build()});

            var lociToExclude = donorLociToExclude.Union(patientLociToExclude);

            var matchDetails = await MatchProbabilityService.CalculateMatchProbability(matchProbabilityInput);

            lociToExclude.Should()
                .OnlyContain(l => matchDetails.ZeroMismatchProbabilityPerLocus.GetLocus(l) == null, "excluded loci should be null");
        }
    }
}
