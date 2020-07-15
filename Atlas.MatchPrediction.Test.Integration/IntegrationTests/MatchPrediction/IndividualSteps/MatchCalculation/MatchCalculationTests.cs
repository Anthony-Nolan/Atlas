using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.HlaMetadataDictionary.Test.IntegrationTests.TestHelpers.FileBackedStorageStubs;
using Atlas.MatchPrediction.Services.MatchCalculation;
using Atlas.MatchPrediction.Test.Integration.Resources;
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

        private static readonly LociInfo<int?> TenOutOfTenMatch = new LociInfo<int?>
            {A = 2, B = 2, C = 2, Dpb1 = null, Dqb1 = 2, Drb1 = 2};

        private static readonly LociInfo<int?> SingleMismatchAtA = new LociInfo<int?>
            {A = 1, B = 2, C = 2, Dpb1 = null, Dqb1 = 2, Drb1 = 2};

        private static readonly LociInfo<int?> DoubleMismatchAtA = new LociInfo<int?>
            {A = 0, B = 2, C = 2, Dpb1 = null, Dqb1 = 2, Drb1 = 2};


        private static readonly ISet<Locus> allowedLoci = new HashSet<Locus> {Locus.A, Locus.B, Locus.C, Locus.Dqb1, Locus.Drb1};

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            matchCalculationService =
                DependencyInjection.DependencyInjection.Provider.GetService<IMatchCalculationService>();
        }

        [Test]
        public async Task MatchAtPGroupLevel_WhenIdenticalGenotypes_IsTenOutOfTenMatch()
        {
            var matchDetails = await matchCalculationService
                .MatchAtPGroupLevel(
                    DefaultUnambiguousAllelesBuilder.Build(),
                    DefaultUnambiguousAllelesBuilder.Build(),
                    HlaNomenclatureVersion,
                    allowedLoci,
                    allowedLoci);

            matchDetails.MatchCounts.Should().BeEquivalentTo(TenOutOfTenMatch);
        }

        [Test]
        public async Task MatchAtPGroupLevel_WhenGenotypesWithDifferentAllelesWithSameGGroup_IsTenOutOfTenMatch()
        {
            var donorGenotype = DefaultUnambiguousAllelesBuilder.WithDataAt(Locus.B, LocusPosition.Two, "15:228").Build();
            var patientGenotype = DefaultUnambiguousAllelesBuilder.WithDataAt(Locus.B, LocusPosition.Two, "15:146").Build();

            var matchDetails = await matchCalculationService.MatchAtPGroupLevel(
                patientGenotype,
                donorGenotype, 
                HlaNomenclatureVersion,
                allowedLoci,
                allowedLoci);

            matchDetails.MatchCounts.Should().BeEquivalentTo(TenOutOfTenMatch);
        }

        [Test]
        public async Task MatchAtPGroupLevel_WhenGenotypesWithDifferentAllelesWithSamePGroup_IsTenOutOfTenMatch()
        {
            var donorGenotype = DefaultUnambiguousAllelesBuilder.WithDataAt(Locus.A, LocusPosition.Two, "02:09").Build();
            var patientGenotype = DefaultUnambiguousAllelesBuilder.WithDataAt(Locus.A, LocusPosition.Two, "02:66").Build();

            var matchDetails = await matchCalculationService.MatchAtPGroupLevel(
                patientGenotype,
                donorGenotype,
                HlaNomenclatureVersion,
                allowedLoci,
                allowedLoci);

            matchDetails.MatchCounts.Should().BeEquivalentTo(TenOutOfTenMatch);
        }

        [Test]
        public async Task MatchAtPGroupLevel_WhenDonorIsSerologicallyTyped_AndDonorHasMatchingAllele_IsTenOutOfTenMatch()
        {
            var donorGenotype = DefaultUnambiguousAllelesBuilder.WithDataAt(Locus.A, LocusPosition.One, "2").Build();
            var patientGenotype = DefaultUnambiguousAllelesBuilder.WithDataAt(Locus.A, LocusPosition.One, "02:09").Build();

            var matchDetails = await matchCalculationService.MatchAtPGroupLevel(
                patientGenotype,
                donorGenotype,
                HlaNomenclatureVersion,
                allowedLoci,
                allowedLoci);

            matchDetails.MatchCounts.Should().BeEquivalentTo(TenOutOfTenMatch);
        }

        [Test]
        public async Task MatchAtPGroupLevel_WhenGenotypesDifferInPhase_IsTenOutOfTenMatch()
        {
            var donorGenotype = DefaultUnambiguousAllelesBuilder
                .WithDataAt(Locus.A, Alleles.UnambiguousAlleleDetails.A.Position2.Allele, Alleles.UnambiguousAlleleDetails.A.Position1.Allele)
                .Build();

            var matchDetails = await matchCalculationService
                .MatchAtPGroupLevel(
                    DefaultUnambiguousAllelesBuilder.Build(),
                    donorGenotype,
                    HlaNomenclatureVersion,
                    allowedLoci,
                    allowedLoci);

            matchDetails.MatchCounts.Should().BeEquivalentTo(TenOutOfTenMatch);
        }

        [Test]
        public async Task MatchAtPGroupLevel_WhenPatientGenotypeHomozygous_AndMatchesExactlyOneOfPatientHla_IsNineOutOfTenMatch()
        {
            var patientGenotype = DefaultUnambiguousAllelesBuilder.WithDataAt(Locus.A, Alleles.UnambiguousAlleleDetails.A.Position1.Allele).Build();

            var matchDetails = await matchCalculationService
                .MatchAtPGroupLevel(
                    patientGenotype,
                    DefaultUnambiguousAllelesBuilder.Build(),
                    HlaNomenclatureVersion,
                    allowedLoci,
                    allowedLoci);

            matchDetails.MatchCounts.Should().BeEquivalentTo(SingleMismatchAtA);
        }

        [Test]
        public async Task MatchAtPGroupLevel_WhenDonorGenotypeHomozygous_AndMatchesExactlyOneOfDonorHla_IsNineOutOfTenMatch()
        {
            var donorGenotype = DefaultUnambiguousAllelesBuilder.WithDataAt(Locus.A, Alleles.UnambiguousAlleleDetails.A.Position1.Allele).Build();

            var matchDetails = await matchCalculationService
                .MatchAtPGroupLevel(
                    DefaultUnambiguousAllelesBuilder.Build(),
                    donorGenotype,
                    HlaNomenclatureVersion,
                    allowedLoci,
                    allowedLoci);

            matchDetails.MatchCounts.Should().BeEquivalentTo(SingleMismatchAtA);
        }

        [Test]
        public async Task MatchAtPGroupLevel_WhenDonorGenotypeHasLocusMismatch_IsEightOutOfTenMatch()
        {
            var donorGenotype = DefaultUnambiguousAllelesBuilder.WithDataAt(Locus.A, "11:03").Build();
            var patientGenotype = DefaultUnambiguousAllelesBuilder.WithDataAt(Locus.A, "02:09").Build();

            var matchDetails = await matchCalculationService.MatchAtPGroupLevel(
                patientGenotype,
                donorGenotype,
                HlaNomenclatureVersion,
                allowedLoci,
                allowedLoci);

            matchDetails.MatchCounts.Should().BeEquivalentTo(DoubleMismatchAtA);
        }

        private static PhenotypeInfoBuilder<string> DefaultUnambiguousAllelesBuilder =>
            new PhenotypeInfoBuilder<string>(Alleles.UnambiguousAlleleDetails.Alleles());
    }
}