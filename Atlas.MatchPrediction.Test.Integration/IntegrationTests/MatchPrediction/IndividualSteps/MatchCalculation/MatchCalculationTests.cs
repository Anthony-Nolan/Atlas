using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.HlaMetadataDictionary.Test.IntegrationTests.TestHelpers.FileBackedStorageStubs;
using Atlas.MatchPrediction.Config;
using Atlas.MatchPrediction.Services.MatchCalculation;
using Atlas.MatchPrediction.Test.Integration.Resources;
using Atlas.MatchPrediction.Test.TestHelpers.Builders;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Integration.IntegrationTests.MatchPrediction.IndividualSteps.MatchCalculation
{
    [TestFixture]
    public class MatchCalculationTests
    {
        private IMatchCalculationService matchCalculationService;

        private const string HlaNomenclatureVersion = FileBackedHlaMetadataRepositoryBaseReader.OlderTestHlaVersion;

        private static readonly IEnumerable<Locus> AllowedLoci = LocusSettings.MatchPredictionLoci;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            matchCalculationService =
                DependencyInjection.DependencyInjection.Provider.GetService<IMatchCalculationService>();
        }

        [TestCase(new Locus[0])]
        [TestCase(new[] {Locus.Dqb1})]
        [TestCase(new[] {Locus.C})]
        [TestCase(new[] {Locus.Dqb1, Locus.C})]
        public async Task MatchAtPGroupLevel_WhenIdenticalGenotypes_IsTenOutOfTenMatch(Locus[] lociToExclude)
        {
            var loci = AllowedLoci.Where(l => !lociToExclude.Contains(l)).ToHashSet();

            var matchDetails = await matchCalculationService
                .MatchAtPGroupLevel(
                    DefaultUnambiguousAllelesBuilder.Build(),
                    DefaultUnambiguousAllelesBuilder.Build(),
                    HlaNomenclatureVersion,
                    loci);

            var expectedMatchCounts = new MatchCountsBuilder().ZeroMismatch(loci).Build();

            matchDetails.MatchCounts.Should().BeEquivalentTo(expectedMatchCounts);
        }

        [TestCase(new Locus[0])]
        [TestCase(new[] {Locus.Dqb1})]
        [TestCase(new[] {Locus.C})]
        [TestCase(new[] {Locus.Dqb1, Locus.C})]
        public async Task MatchAtPGroupLevel_WhenGenotypesWithDifferentAllelesWithSameGGroup_IsTenOutOfTenMatch(Locus[] lociToExclude)
        {
            var loci = AllowedLoci.Where(l => !lociToExclude.Contains(l)).ToHashSet();
            var donorGenotype = DefaultUnambiguousAllelesBuilder.WithDataAt(Locus.B, LocusPosition.Two, "15:228").Build();
            var patientGenotype = DefaultUnambiguousAllelesBuilder.WithDataAt(Locus.B, LocusPosition.Two, "15:146").Build();

            var matchDetails = await matchCalculationService.MatchAtPGroupLevel(
                patientGenotype,
                donorGenotype, 
                HlaNomenclatureVersion,
                loci.ToHashSet());

            var expectedMatchCounts = new MatchCountsBuilder().ZeroMismatch(loci).Build();

            matchDetails.MatchCounts.Should().BeEquivalentTo(expectedMatchCounts);
        }

        [TestCase(new Locus[0])]
        [TestCase(new[] {Locus.Dqb1})]
        [TestCase(new[] {Locus.C})]
        [TestCase(new[] {Locus.Dqb1, Locus.C})]
        public async Task MatchAtPGroupLevel_WhenGenotypesWithDifferentAllelesWithSamePGroup_IsTenOutOfTenMatch(Locus[] lociToExclude)
        {
            var loci = AllowedLoci.Where(l => !lociToExclude.Contains(l)).ToHashSet();
            var donorGenotype = DefaultUnambiguousAllelesBuilder.WithDataAt(Locus.A, LocusPosition.Two, "02:09").Build();
            var patientGenotype = DefaultUnambiguousAllelesBuilder.WithDataAt(Locus.A, LocusPosition.Two, "02:66").Build();

            var matchDetails = await matchCalculationService.MatchAtPGroupLevel(
                patientGenotype,
                donorGenotype,
                HlaNomenclatureVersion,
                loci.ToHashSet());

            var expectedMatchCounts = new MatchCountsBuilder().ZeroMismatch(loci).Build();

            matchDetails.MatchCounts.Should().BeEquivalentTo(expectedMatchCounts);
        }

        [TestCase(new Locus[0])]
        [TestCase(new[] {Locus.Dqb1})]
        [TestCase(new[] {Locus.C})]
        [TestCase(new[] {Locus.Dqb1, Locus.C})]
        public async Task MatchAtPGroupLevel_WhenDonorIsSerologicallyTyped_AndDonorHasMatchingAllele_IsTenOutOfTenMatch(Locus[] lociToExclude)
        {
            var loci = AllowedLoci.Where(l => !lociToExclude.Contains(l)).ToHashSet();
            var donorGenotype = DefaultUnambiguousAllelesBuilder.WithDataAt(Locus.A, LocusPosition.One, "2").Build();
            var patientGenotype = DefaultUnambiguousAllelesBuilder.WithDataAt(Locus.A, LocusPosition.One, "02:09").Build();

            var matchDetails = await matchCalculationService.MatchAtPGroupLevel(
                patientGenotype,
                donorGenotype,
                HlaNomenclatureVersion,
                loci.ToHashSet());

            var expectedMatchCounts = new MatchCountsBuilder().ZeroMismatch(loci).Build();

            matchDetails.MatchCounts.Should().BeEquivalentTo(expectedMatchCounts);
        }

        [TestCase(new Locus[0])]
        [TestCase(new[] {Locus.Dqb1})]
        [TestCase(new[] {Locus.C})]
        [TestCase(new[] {Locus.Dqb1, Locus.C})]
        public async Task MatchAtPGroupLevel_WhenGenotypesDifferInPhase_IsTenOutOfTenMatch(Locus[] lociToExclude)
        {
            var loci = AllowedLoci.Where(l => !lociToExclude.Contains(l)).ToHashSet();
            var donorGenotype = DefaultUnambiguousAllelesBuilder
                .WithDataAt(Locus.A, Alleles.UnambiguousAlleleDetails.A.Position2.Allele, Alleles.UnambiguousAlleleDetails.A.Position1.Allele)
                .Build();

            var matchDetails = await matchCalculationService
                .MatchAtPGroupLevel(
                    DefaultUnambiguousAllelesBuilder.Build(),
                    donorGenotype,
                    HlaNomenclatureVersion,
                    loci.ToHashSet());

            var expectedMatchCounts = new MatchCountsBuilder().ZeroMismatch(loci).Build();

            matchDetails.MatchCounts.Should().BeEquivalentTo(expectedMatchCounts);
        }

        [TestCase(new Locus[0])]
        [TestCase(new[] {Locus.Dqb1})]
        [TestCase(new[] {Locus.C})]
        [TestCase(new[] {Locus.Dqb1, Locus.C})]
        public async Task MatchAtPGroupLevel_WhenPatientGenotypeHomozygous_AndMatchesExactlyOneOfPatientHla_IsNineOutOfTenMatch(Locus[] lociToExclude)
        {
            var loci = AllowedLoci.Where(l => !lociToExclude.Contains(l)).ToHashSet();
            var patientGenotype = DefaultUnambiguousAllelesBuilder.WithDataAt(Locus.A, Alleles.UnambiguousAlleleDetails.A.Position1.Allele).Build();

            var matchDetails = await matchCalculationService
                .MatchAtPGroupLevel(
                    patientGenotype,
                    DefaultUnambiguousAllelesBuilder.Build(),
                    HlaNomenclatureVersion,
                    loci.ToHashSet());

            var expectedMatchCounts = new MatchCountsBuilder().ZeroMismatch(loci).WithSingleMismatchAt(Locus.A).Build();

            matchDetails.MatchCounts.Should().BeEquivalentTo(expectedMatchCounts);
        }

        [TestCase(new Locus[0])]
        [TestCase(new[] {Locus.Dqb1})]
        [TestCase(new[] {Locus.C})]
        [TestCase(new[] {Locus.Dqb1, Locus.C})]
        public async Task MatchAtPGroupLevel_WhenDonorGenotypeHomozygous_AndMatchesExactlyOneOfDonorHla_IsNineOutOfTenMatch(Locus[] lociToExclude)
        {
            var loci = AllowedLoci.Where(l => !lociToExclude.Contains(l)).ToHashSet();
            var donorGenotype = DefaultUnambiguousAllelesBuilder.WithDataAt(Locus.A, Alleles.UnambiguousAlleleDetails.A.Position1.Allele).Build();

            var matchDetails = await matchCalculationService
                .MatchAtPGroupLevel(
                    DefaultUnambiguousAllelesBuilder.Build(),
                    donorGenotype,
                    HlaNomenclatureVersion,
                    loci.ToHashSet());

            var expectedMatchCounts = new MatchCountsBuilder().ZeroMismatch(loci).WithSingleMismatchAt(Locus.A).Build();

            matchDetails.MatchCounts.Should().BeEquivalentTo(expectedMatchCounts);
        }

        [TestCase(new Locus[0])]
        [TestCase(new[] {Locus.Dqb1})]
        [TestCase(new[] {Locus.C})]
        [TestCase(new[] {Locus.Dqb1, Locus.C})]
        public async Task MatchAtPGroupLevel_WhenDonorGenotypeHasLocusMismatch_IsEightOutOfTenMatch(Locus[] lociToExclude)
        {
            var loci = AllowedLoci.Where(l => !lociToExclude.Contains(l)).ToHashSet();
            var donorGenotype = DefaultUnambiguousAllelesBuilder.WithDataAt(Locus.A, "11:03").Build();
            var patientGenotype = DefaultUnambiguousAllelesBuilder.WithDataAt(Locus.A, "02:09").Build();

            var matchDetails = await matchCalculationService.MatchAtPGroupLevel(
                patientGenotype,
                donorGenotype,
                HlaNomenclatureVersion,
                loci.ToHashSet());

            var expectedMatchCounts = new MatchCountsBuilder().ZeroMismatch(loci).WithDoubleMismatchAt(Locus.A).Build();

            matchDetails.MatchCounts.Should().BeEquivalentTo(expectedMatchCounts);
        }

        private static PhenotypeInfoBuilder<string> DefaultUnambiguousAllelesBuilder =>
            new PhenotypeInfoBuilder<string>(Alleles.UnambiguousAlleleDetails.Alleles());
    }
}