using System.Collections.Generic;
using System.Linq;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.MatchPrediction.Config;
using Atlas.MatchPrediction.Services.MatchCalculation;
using Atlas.MatchPrediction.Test.Integration.Resources.Alleles;
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

        private static readonly ISet<Locus> AllowedLoci = LocusSettings.MatchPredictionLoci;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            matchCalculationService = DependencyInjection.DependencyInjection.Provider.GetService<IMatchCalculationService>();
        }

        [TestCase(new Locus[0])]
        [TestCase(new[] {Locus.Dqb1})]
        [TestCase(new[] {Locus.C})]
        [TestCase(new[] {Locus.Dqb1, Locus.C})]
        public void CalculateMatchCounts_Fast_WhenIdenticalGenotypes_IsTenOutOfTenMatch(Locus[] lociToExclude)
        {
            var loci = AllowedLoci.Where(l => !lociToExclude.Contains(l)).ToHashSet();

            var matchCounts = matchCalculationService
                .CalculateMatchCounts_Fast(
                    DefaultGGroupsBuilder.Build(),
                    DefaultGGroupsBuilder.Build(),
                    loci);

            var expectedMatchCounts = new MatchCountsBuilder().ZeroMismatch(loci).Build();

            matchCounts.Should().BeEquivalentTo(expectedMatchCounts);
        }

        [TestCase(new Locus[0])]
        [TestCase(new[] {Locus.Dqb1})]
        [TestCase(new[] {Locus.C})]
        [TestCase(new[] {Locus.Dqb1, Locus.C})]
        public void CalculateMatchCounts_Fast_WhenGenotypesDifferInPhase_IsTenOutOfTenMatch(Locus[] lociToExclude)
        {
            var loci = AllowedLoci.Where(l => !lociToExclude.Contains(l)).ToHashSet();
            var donorGenotype = DefaultGGroupsBuilder
                .WithDataAt(Locus.A, Alleles.UnambiguousAlleleDetails.A.Position2.GGroup, Alleles.UnambiguousAlleleDetails.A.Position1.GGroup)
                .Build();

            var matchCounts = matchCalculationService
                .CalculateMatchCounts_Fast(
                    DefaultGGroupsBuilder.Build(),
                    donorGenotype,
                    loci.ToHashSet());

            var expectedMatchCounts = new MatchCountsBuilder().ZeroMismatch(loci).Build();

            matchCounts.Should().BeEquivalentTo(expectedMatchCounts);
        }

        [TestCase(new Locus[0])]
        [TestCase(new[] {Locus.Dqb1})]
        [TestCase(new[] {Locus.C})]
        [TestCase(new[] {Locus.Dqb1, Locus.C})]
        public void CalculateMatchCounts_Fast_WhenPatientGenotypeHomozygous_AndMatchesExactlyOneOfPatientHla_IsNineOutOfTenMatch(
            Locus[] lociToExclude)
        {
            var loci = AllowedLoci.Where(l => !lociToExclude.Contains(l)).ToHashSet();
            var patientGenotype = DefaultGGroupsBuilder.WithDataAt(Locus.A, Alleles.UnambiguousAlleleDetails.A.Position1.GGroup).Build();

            var matchCounts = matchCalculationService
                .CalculateMatchCounts_Fast(
                    patientGenotype,
                    DefaultGGroupsBuilder.Build(),
                    loci.ToHashSet());

            var expectedMatchCounts = new MatchCountsBuilder().ZeroMismatch(loci).WithSingleMismatchAt(Locus.A).Build();

            matchCounts.Should().BeEquivalentTo(expectedMatchCounts);
        }

        [TestCase(new Locus[0])]
        [TestCase(new[] {Locus.Dqb1})]
        [TestCase(new[] {Locus.C})]
        [TestCase(new[] {Locus.Dqb1, Locus.C})]
        public void CalculateMatchCounts_Fast_WhenDonorGenotypeHomozygous_AndMatchesExactlyOneOfDonorHla_IsNineOutOfTenMatch(Locus[] lociToExclude)
        {
            var loci = AllowedLoci.Where(l => !lociToExclude.Contains(l)).ToHashSet();
            var donorGenotype = DefaultGGroupsBuilder.WithDataAt(Locus.A, Alleles.UnambiguousAlleleDetails.A.Position1.GGroup).Build();

            var matchCounts = matchCalculationService
                .CalculateMatchCounts_Fast(
                    DefaultGGroupsBuilder.Build(),
                    donorGenotype,
                    loci.ToHashSet());

            var expectedMatchCounts = new MatchCountsBuilder().ZeroMismatch(loci).WithSingleMismatchAt(Locus.A).Build();

            matchCounts.Should().BeEquivalentTo(expectedMatchCounts);
        }

        [TestCase(new Locus[0])]
        [TestCase(new[] {Locus.Dqb1})]
        [TestCase(new[] {Locus.C})]
        [TestCase(new[] {Locus.Dqb1, Locus.C})]
        public void CalculateMatchCounts_Fast_WhenDonorGenotypeHasLocusMismatch_IsEightOutOfTenMatch(Locus[] lociToExclude)
        {
            var loci = AllowedLoci.Where(l => !lociToExclude.Contains(l)).ToHashSet();
            var donorGenotype = DefaultGGroupsBuilder.WithDataAt(Locus.A, "01:120").Build();
            var patientGenotype = DefaultGGroupsBuilder.WithDataAt(Locus.A, "01:84").Build();

            var matchCounts = matchCalculationService.CalculateMatchCounts_Fast(
                patientGenotype,
                donorGenotype,
                loci.ToHashSet());

            var expectedMatchCounts = new MatchCountsBuilder().ZeroMismatch(loci).WithDoubleMismatchAt(Locus.A).Build();

            matchCounts.Should().BeEquivalentTo(expectedMatchCounts);
        }

        private static PhenotypeInfoBuilder<string> DefaultGGroupsBuilder =>
            new PhenotypeInfoBuilder<string>(Alleles.UnambiguousAlleleDetails.GGroups());
    }
}