using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.MatchPrediction.Data.Models;
using Atlas.MatchPrediction.Services.GenotypeLikelihood;
using Atlas.MatchPrediction.Services.HaplotypeFrequencies;
using Atlas.MatchPrediction.Test.Integration.Resources;
using Atlas.MatchPrediction.Test.Integration.TestHelpers.Builders.FrequencySetFile;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Integration.IntegrationTests.GenotypeLikelihood
{
    ///<summary>
    /// These tests are snapshots based on some manually calculated frequencies/expectations.
    /// Any tests of the GenotypeLikelihood calculator that are not such tests should be added elsewhere.
    ///</summary>
    [TestFixture]
    public class GenotypeLikelihoodTests
    {
        private IFrequencySetService importService;
        private IGenotypeLikelihoodService likelihoodService;

        private readonly string a1 = Alleles.UnambiguousAlleleDetails.GGroups().A.Position1;
        private readonly string a2 = Alleles.UnambiguousAlleleDetails.GGroups().A.Position2;
        private readonly string b1 = Alleles.UnambiguousAlleleDetails.GGroups().B.Position1;
        private readonly string b2 = Alleles.UnambiguousAlleleDetails.GGroups().B.Position2;
        private readonly string c1 = Alleles.UnambiguousAlleleDetails.GGroups().C.Position1;
        private readonly string c2 = Alleles.UnambiguousAlleleDetails.GGroups().C.Position2;
        private readonly string dqb11 = Alleles.UnambiguousAlleleDetails.GGroups().Dqb1.Position1;
        private readonly string dqb12 = Alleles.UnambiguousAlleleDetails.GGroups().Dqb1.Position2;
        private readonly string drb11 = Alleles.UnambiguousAlleleDetails.GGroups().Drb1.Position1;
        private readonly string drb12 = Alleles.UnambiguousAlleleDetails.GGroups().Drb1.Position2;

        [SetUp]
        public async Task SetUp()
        {
            importService = DependencyInjection.DependencyInjection.Provider.GetService<IFrequencySetService>();
            likelihoodService = DependencyInjection.DependencyInjection.Provider.GetService<IGenotypeLikelihoodService>();

            // 32 possible haplotypes for a single unambiguous genotype.
            var allPossibleHaplotypes = new List<HaplotypeFrequency>
            {
                new HaplotypeFrequency {A = a2, B = b1, C = c1, DQB1 = dqb11, DRB1 = drb11, Frequency = 0.32m},
                new HaplotypeFrequency {A = a1, B = b2, C = c2, DQB1 = dqb12, DRB1 = drb12, Frequency = 0.31m},
                new HaplotypeFrequency {A = a2, B = b1, C = c1, DQB1 = dqb11, DRB1 = drb12, Frequency = 0.30m},
                new HaplotypeFrequency {A = a1, B = b2, C = c2, DQB1 = dqb12, DRB1 = drb11, Frequency = 0.29m},
                new HaplotypeFrequency {A = a2, B = b1, C = c1, DQB1 = dqb12, DRB1 = drb11, Frequency = 0.28m},
                new HaplotypeFrequency {A = a1, B = b2, C = c2, DQB1 = dqb11, DRB1 = drb12, Frequency = 0.27m},
                new HaplotypeFrequency {A = a2, B = b1, C = c1, DQB1 = dqb12, DRB1 = drb12, Frequency = 0.26m},
                new HaplotypeFrequency {A = a1, B = b2, C = c2, DQB1 = dqb11, DRB1 = drb11, Frequency = 0.25m},
                new HaplotypeFrequency {A = a2, B = b1, C = c2, DQB1 = dqb11, DRB1 = drb11, Frequency = 0.24m},
                new HaplotypeFrequency {A = a1, B = b2, C = c1, DQB1 = dqb12, DRB1 = drb12, Frequency = 0.23m},
                new HaplotypeFrequency {A = a2, B = b1, C = c2, DQB1 = dqb11, DRB1 = drb12, Frequency = 0.22m},
                new HaplotypeFrequency {A = a1, B = b2, C = c1, DQB1 = dqb12, DRB1 = drb11, Frequency = 0.21m},
                new HaplotypeFrequency {A = a2, B = b1, C = c2, DQB1 = dqb12, DRB1 = drb11, Frequency = 0.20m},
                new HaplotypeFrequency {A = a1, B = b2, C = c1, DQB1 = dqb11, DRB1 = drb12, Frequency = 0.19m},
                new HaplotypeFrequency {A = a2, B = b1, C = c2, DQB1 = dqb12, DRB1 = drb12, Frequency = 0.18m},
                new HaplotypeFrequency {A = a1, B = b2, C = c1, DQB1 = dqb11, DRB1 = drb11, Frequency = 0.17m},
                new HaplotypeFrequency {A = a2, B = b2, C = c1, DQB1 = dqb11, DRB1 = drb11, Frequency = 0.16m},
                new HaplotypeFrequency {A = a1, B = b1, C = c2, DQB1 = dqb12, DRB1 = drb12, Frequency = 0.15m},
                new HaplotypeFrequency {A = a2, B = b2, C = c1, DQB1 = dqb11, DRB1 = drb12, Frequency = 0.14m},
                new HaplotypeFrequency {A = a1, B = b1, C = c2, DQB1 = dqb12, DRB1 = drb11, Frequency = 0.13m},
                new HaplotypeFrequency {A = a2, B = b2, C = c1, DQB1 = dqb12, DRB1 = drb11, Frequency = 0.12m},
                new HaplotypeFrequency {A = a1, B = b1, C = c2, DQB1 = dqb11, DRB1 = drb12, Frequency = 0.11m},
                new HaplotypeFrequency {A = a2, B = b2, C = c1, DQB1 = dqb12, DRB1 = drb12, Frequency = 0.10m},
                new HaplotypeFrequency {A = a1, B = b1, C = c2, DQB1 = dqb11, DRB1 = drb11, Frequency = 0.09m},
                new HaplotypeFrequency {A = a2, B = b2, C = c2, DQB1 = dqb11, DRB1 = drb11, Frequency = 0.08m},
                new HaplotypeFrequency {A = a1, B = b1, C = c1, DQB1 = dqb12, DRB1 = drb12, Frequency = 0.07m},
                new HaplotypeFrequency {A = a2, B = b2, C = c2, DQB1 = dqb11, DRB1 = drb12, Frequency = 0.06m},
                new HaplotypeFrequency {A = a1, B = b1, C = c1, DQB1 = dqb12, DRB1 = drb11, Frequency = 0.05m},
                new HaplotypeFrequency {A = a2, B = b2, C = c2, DQB1 = dqb12, DRB1 = drb11, Frequency = 0.04m},
                new HaplotypeFrequency {A = a1, B = b1, C = c1, DQB1 = dqb11, DRB1 = drb12, Frequency = 0.03m},
                new HaplotypeFrequency {A = a2, B = b2, C = c2, DQB1 = dqb12, DRB1 = drb12, Frequency = 0.02m},
                new HaplotypeFrequency {A = a1, B = b1, C = c1, DQB1 = dqb11, DRB1 = drb11, Frequency = 0.01m}
            };

            await ImportFrequencies(allPossibleHaplotypes);
        }

        [Test]
        public async Task CalculateLikelihood_WhenAllLociAreHeterozygous_ReturnsExpectedLikelihood()
        {
            var genotypeInput = DefaultUnambiguousGGroupsBuilder.Build();

            var likelihoodResponse = await likelihoodService.CalculateLikelihood(genotypeInput);

            likelihoodResponse.Should().Be(1.1424m);
        }

        [TestCase(Locus.A, 0.2736)]
        [TestCase(Locus.B, 0.3536)]
        [TestCase(Locus.C, 0.5136)]
        [TestCase(Locus.Dqb1, 0.5552)]
        [TestCase(Locus.Drb1, 0.5664)]
        public async Task CalculateLikelihood_WhenLocusIsHomozygous_ReturnsExpectedLikelihood(Locus homozygousLocus, decimal expectedLikelihood)
        {
            var genotype = DefaultUnambiguousGGroupsBuilder
                .WithDataAt(homozygousLocus, Alleles.UnambiguousAlleleDetails.GetPosition(homozygousLocus, LocusPosition.One).GGroup)
                .Build();

            var likelihoodResponse = await likelihoodService.CalculateLikelihood(genotype);

            likelihoodResponse.Should().Be(expectedLikelihood);
        }

        [TestCase(new[] {Locus.A, Locus.C}, 0.06)]
        [TestCase(new[] {Locus.B, Locus.Drb1}, 0.1616)]
        [TestCase(new[] {Locus.C, Locus.Dqb1, Locus.B}, 0.0252)]
        [TestCase(new[] {Locus.Dqb1, Locus.A, Locus.Drb1, Locus.C}, 0.0034)]
        public async Task CalculateLikelihood_WhenMultipleLocusAreHomozygous_ReturnsExpectedLikelihood(
            Locus[] homozygousLoci,
            decimal expectedLikelihood)
        {
            var genotype = DefaultUnambiguousGGroupsBuilder.Build();

            foreach (var homozygousLocus in homozygousLoci)
            {
                genotype.SetPosition(homozygousLocus, LocusPosition.Two,
                    genotype.GetPosition(homozygousLocus, LocusPosition.One));
            }

            var likelihoodResponse = await likelihoodService.CalculateLikelihood(genotype);

            likelihoodResponse.Should().Be(expectedLikelihood);
        }

        //TODO: ATLAS-345: This test will no longer be correct once we handle untyped input loci.
        [TestCase(Locus.C)]
        [TestCase(Locus.Dqb1)]
        public async Task CalculateLikelihood_WhenGenotypeHasNullLoci_ReturnsZeroLikelihood(Locus locusToBeNull)
        {
            var genotype = DefaultUnambiguousGGroupsBuilder.WithDataAt(locusToBeNull, (string)null).Build();

            var likelihoodResponse = await likelihoodService.CalculateLikelihood(genotype);

            likelihoodResponse.Should().Be(0m);
        }

        [TestCase(Locus.A)]
        [TestCase(Locus.B)]
        [TestCase(Locus.C)]
        [TestCase(Locus.Dqb1)]
        [TestCase(Locus.Drb1)]
        public async Task CalculateLikelihood_WhenNoHaplotypesAreRepresentedInDatabase_ReturnsZeroLikelihood(Locus unrepresentedLocus)
        {
            var genotype = DefaultUnambiguousGGroupsBuilder.WithDataAt(unrepresentedLocus, "un-represented").Build();

            var likelihoodResponse = await likelihoodService.CalculateLikelihood(genotype);

            likelihoodResponse.Should().Be(0m);
        }

        [Test]
        public async Task CalculateLikelihood_WhenOnlySomeHaplotypesAreRepresentedInDatabase_ReturnsExpectedLikelihood()
        {
            // 16 of the possible 32 haplotypes for a single unambiguous genotype.
            var haplotypesWith16Missing = new List<HaplotypeFrequency>
            {
                new HaplotypeFrequency {A = a2, B = b1, C = c1, DQB1 = dqb11, DRB1 = drb11, Frequency = 0.16m},
                new HaplotypeFrequency {A = a1, B = b2, C = c2, DQB1 = dqb12, DRB1 = drb12, Frequency = 0.15m},
                new HaplotypeFrequency {A = a2, B = b1, C = c1, DQB1 = dqb11, DRB1 = drb12, Frequency = 0.14m},
                new HaplotypeFrequency {A = a1, B = b2, C = c2, DQB1 = dqb12, DRB1 = drb11, Frequency = 0.13m},
                new HaplotypeFrequency {A = a2, B = b1, C = c1, DQB1 = dqb12, DRB1 = drb11, Frequency = 0.12m},
                new HaplotypeFrequency {A = a1, B = b2, C = c2, DQB1 = dqb11, DRB1 = drb12, Frequency = 0.11m},
                new HaplotypeFrequency {A = a2, B = b1, C = c1, DQB1 = dqb12, DRB1 = drb12, Frequency = 0.10m},
                new HaplotypeFrequency {A = a1, B = b2, C = c2, DQB1 = dqb11, DRB1 = drb11, Frequency = 0.09m},
                new HaplotypeFrequency {A = a2, B = b1, C = c2, DQB1 = dqb11, DRB1 = drb11, Frequency = 0.08m},
                new HaplotypeFrequency {A = a1, B = b2, C = c1, DQB1 = dqb12, DRB1 = drb12, Frequency = 0.07m},
                new HaplotypeFrequency {A = a2, B = b1, C = c2, DQB1 = dqb11, DRB1 = drb12, Frequency = 0.06m},
                new HaplotypeFrequency {A = a1, B = b2, C = c1, DQB1 = dqb12, DRB1 = drb11, Frequency = 0.05m},
                new HaplotypeFrequency {A = a2, B = b1, C = c2, DQB1 = dqb12, DRB1 = drb11, Frequency = 0.04m},
                new HaplotypeFrequency {A = a1, B = b2, C = c1, DQB1 = dqb11, DRB1 = drb12, Frequency = 0.03m},
                new HaplotypeFrequency {A = a2, B = b1, C = c2, DQB1 = dqb12, DRB1 = drb12, Frequency = 0.02m},
                new HaplotypeFrequency {A = a1, B = b2, C = c1, DQB1 = dqb11, DRB1 = drb11, Frequency = 0.01m}
            };

            await ImportFrequencies(haplotypesWith16Missing);

            var genotype = DefaultUnambiguousGGroupsBuilder.Build();

            var likelihoodResponse = await likelihoodService.CalculateLikelihood(genotype);

            likelihoodResponse.Should().Be(0.1488m);
        }

        private async Task ImportFrequencies(IEnumerable<HaplotypeFrequency> haplotypes)
        {
            using var file = FrequencySetFileBuilder.New(haplotypes).Build();
            await importService.ImportFrequencySet(file);
        }

        private static PhenotypeInfoBuilder<string> DefaultUnambiguousGGroupsBuilder =>
            new PhenotypeInfoBuilder<string>(Alleles.UnambiguousAlleleDetails.GGroups());
    }
}