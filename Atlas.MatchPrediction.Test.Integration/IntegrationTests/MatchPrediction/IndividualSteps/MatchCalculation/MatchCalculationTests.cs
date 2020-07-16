using System.Collections.Generic;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.HlaMetadataDictionary.Test.IntegrationTests.TestHelpers.FileBackedStorageStubs;
using Atlas.MatchPrediction.Config;
using Atlas.MatchPrediction.Services.MatchCalculation;
using Atlas.MatchPrediction.Test.Integration.Resources;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MoreLinq.Extensions;
using NUnit.Framework;

namespace Atlas.MatchPrediction.Test.Integration.IntegrationTests.MatchPrediction.IndividualSteps.MatchCalculation
{
    [TestFixture]
    public class MatchCalculationTests
    {
        private IMatchCalculationService matchCalculationService;

        private const string HlaNomenclatureVersion = FileBackedHlaMetadataRepositoryBaseReader.OlderTestHlaVersion;

        private static readonly LociInfo<int?> TenOutOfTenMatch = new LociInfo<int?> {A = 2, B = 2, C = 2, Dpb1 = null, Dqb1 = 2, Drb1 = 2};

        private static readonly LociInfo<int?> SingleMismatchAtA = new LociInfo<int?> {A = 1, B = 2, C = 2, Dpb1 = null, Dqb1 = 2, Drb1 = 2};

        private static readonly LociInfo<int?> DoubleMismatchAtA = new LociInfo<int?> {A = 0, B = 2, C = 2, Dpb1 = null, Dqb1 = 2, Drb1 = 2};


        private static readonly ISet<Locus> AllowedLoci = LocusSettings.MatchPredictionLoci.ToHashSet();

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
                    DefaultGGroupsBuilder.Build(),
                    DefaultGGroupsBuilder.Build(),
                    HlaNomenclatureVersion,
                    AllowedLoci,
                    AllowedLoci);

            matchDetails.MatchCounts.Should().BeEquivalentTo(TenOutOfTenMatch);
        }

        [Test]
        public async Task MatchAtPGroupLevel_WhenGenotypesDifferInPhase_IsTenOutOfTenMatch()
        {
            var donorGenotype = DefaultGGroupsBuilder
                .WithDataAt(Locus.A, Alleles.UnambiguousAlleleDetails.A.Position2.GGroup, Alleles.UnambiguousAlleleDetails.A.Position1.GGroup)
                .Build();

            var matchDetails = await matchCalculationService
                .MatchAtPGroupLevel(
                    DefaultGGroupsBuilder.Build(),
                    donorGenotype,
                    HlaNomenclatureVersion,
                    AllowedLoci,
                    AllowedLoci);

            matchDetails.MatchCounts.Should().BeEquivalentTo(TenOutOfTenMatch);
        }

        [Test]
        public async Task MatchAtPGroupLevel_WhenPatientGenotypeHomozygous_AndMatchesExactlyOneOfPatientHla_IsNineOutOfTenMatch()
        {
            var patientGenotype = DefaultGGroupsBuilder.WithDataAt(Locus.A, Alleles.UnambiguousAlleleDetails.A.Position1.GGroup).Build();

            var matchDetails = await matchCalculationService
                .MatchAtPGroupLevel(
                    patientGenotype,
                    DefaultGGroupsBuilder.Build(),
                    HlaNomenclatureVersion,
                    AllowedLoci,
                    AllowedLoci);

            matchDetails.MatchCounts.Should().BeEquivalentTo(SingleMismatchAtA);
        }

        [Test]
        public async Task MatchAtPGroupLevel_WhenDonorGenotypeHomozygous_AndMatchesExactlyOneOfDonorHla_IsNineOutOfTenMatch()
        {
            var donorGenotype = DefaultGGroupsBuilder.WithDataAt(Locus.A, Alleles.UnambiguousAlleleDetails.A.Position1.GGroup).Build();

            var matchDetails = await matchCalculationService
                .MatchAtPGroupLevel(
                    DefaultGGroupsBuilder.Build(),
                    donorGenotype,
                    HlaNomenclatureVersion,
                    AllowedLoci,
                    AllowedLoci);

            matchDetails.MatchCounts.Should().BeEquivalentTo(SingleMismatchAtA);
        }

        [Test]
        public async Task MatchAtPGroupLevel_WhenDonorGenotypeHasLocusMismatch_IsEightOutOfTenMatch()
        {
            var donorGenotype = DefaultGGroupsBuilder.WithDataAt(Locus.A, "01:120").Build();
            var patientGenotype = DefaultGGroupsBuilder.WithDataAt(Locus.A, "01:84").Build();

            var matchDetails = await matchCalculationService.MatchAtPGroupLevel(
                patientGenotype,
                donorGenotype,
                HlaNomenclatureVersion,
                AllowedLoci,
                AllowedLoci);

            matchDetails.MatchCounts.Should().BeEquivalentTo(DoubleMismatchAtA);
        }

        private static PhenotypeInfoBuilder<string> DefaultGGroupsBuilder =>
            new PhenotypeInfoBuilder<string>(Alleles.UnambiguousAlleleDetails.GGroups());
    }
}