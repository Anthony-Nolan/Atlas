using Autofac;
using FluentAssertions;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Repositories;
using Nova.SearchAlgorithm.Services.Matching;
using Nova.SearchAlgorithm.Test.Integration.TestHelpers.Builders;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.Test.Integration.IntegrationTests.Matching
{
    public enum HomozygousBy
    {
        Typing,
        Expression
    }

    /// <summary>
    /// Check that the matching service returns expected donors for a patient
    /// that is either has the same P group at both positions (homozygous by typing)
    /// or that has one expressing typing and one unambiguously null typing (homozygous by expression).
    /// </summary>
    [TestFixture(Locus.A, HomozygousBy.Typing)]
    [TestFixture(Locus.B, HomozygousBy.Typing)]
    [TestFixture(Locus.C, HomozygousBy.Typing)]
    [TestFixture(Locus.Dqb1, HomozygousBy.Typing)]
    [TestFixture(Locus.Drb1, HomozygousBy.Typing)]
    [TestFixture(Locus.A, HomozygousBy.Expression)]
    [TestFixture(Locus.B, HomozygousBy.Expression)]
    [TestFixture(Locus.C, HomozygousBy.Expression)]
    [TestFixture(Locus.Dqb1, HomozygousBy.Expression)]
    [TestFixture(Locus.Drb1, HomozygousBy.Expression)]
    public class MatchingTestsForHomozygousPatient : IntegrationTestBase
    {
        private const DonorType DefaultDonorType = DonorType.Adult;

        // HLA typing that is unambiguously null-expressing will not have any P groups
        private readonly List<string> emptyPGroupForNullExpressingTyping = new List<string>();
        private readonly List<string> patientPGroup = new List<string> { "patient-p-group" };
        private readonly List<string> nonMatchingPGroupAtPositionOne = new List<string> { "non-matching-p-group-1" };
        private readonly List<string> nonMatchingPGroupAtPositionTwo = new List<string> { "non-matching-p-group-2" };
        private readonly List<string> defaultPGroupForLociNotUnderTest = new List<string> { "dummy-matching-p-group" };

        private readonly Locus locus;
        private readonly HomozygousBy patientHomozygousCategory;
        private IDonorMatchingService matchingService;
        private IEnumerable<int> twoOutOfTwoMatchCountDonors;
        private IEnumerable<int> oneOutOfTwoMatchCountDonors;
        private IEnumerable<int> zeroOutOfTwoMatchCountDonors;

        public MatchingTestsForHomozygousPatient(Locus locus, HomozygousBy patientHomozygousCategory)
        {
            this.locus = locus;
            this.patientHomozygousCategory = patientHomozygousCategory;
        }

        [SetUp]
        public void ResolveSearchRepo()
        {
            matchingService = Container.Resolve<IDonorMatchingService>();
        }

        [OneTimeSetUp]
        public void ImportTestDonors()
        {
            var importRepo = Container.Resolve<IDonorImportRepository>();

            var donorHeterozygousForPatientPGroupAndNull = new InputDonorBuilder(DonorIdGenerator.NextId())
                .WithHlaAtLocus(
                    locus,
                    new ExpandedHla { PGroups = patientPGroup },
                    new ExpandedHla { PGroups = emptyPGroupForNullExpressingTyping }
                )
                .WithDefaultRequiredHla(new ExpandedHla { PGroups = defaultPGroupForLociNotUnderTest })
                .WithDonorType(DefaultDonorType)
                .Build();

            var donorHomozygousForPatientPGroup = new InputDonorBuilder(DonorIdGenerator.NextId())
                .WithHlaAtLocus(
                    locus,
                    new ExpandedHla { PGroups = patientPGroup },
                    new ExpandedHla { PGroups = patientPGroup }
                )
                .WithDefaultRequiredHla(new ExpandedHla { PGroups = defaultPGroupForLociNotUnderTest })
                .WithDonorType(DefaultDonorType)
                .Build();

            var donorHeterozygousForPatientPGroupAndDifferentPGroup = new InputDonorBuilder(DonorIdGenerator.NextId())
                .WithHlaAtLocus(
                    locus,
                    new ExpandedHla { PGroups = patientPGroup },
                    new ExpandedHla { PGroups = nonMatchingPGroupAtPositionTwo }
                )
                .WithDefaultRequiredHla(new ExpandedHla { PGroups = defaultPGroupForLociNotUnderTest })
                .WithDonorType(DefaultDonorType)
                .Build();

            var donorHeterozygousForDifferentPGroupAndNull = new InputDonorBuilder(DonorIdGenerator.NextId())
                .WithHlaAtLocus(
                    locus,
                    new ExpandedHla { PGroups = nonMatchingPGroupAtPositionOne },
                    new ExpandedHla { PGroups = emptyPGroupForNullExpressingTyping }
                )
                .WithDefaultRequiredHla(new ExpandedHla { PGroups = defaultPGroupForLociNotUnderTest })
                .WithDonorType(DefaultDonorType)
                .Build();

            var donorHomozygousForDifferentPGroup = new InputDonorBuilder(DonorIdGenerator.NextId())
                .WithHlaAtLocus(
                    locus,
                    new ExpandedHla { PGroups = nonMatchingPGroupAtPositionOne },
                    new ExpandedHla { PGroups = nonMatchingPGroupAtPositionOne }
                )
                .WithDefaultRequiredHla(new ExpandedHla { PGroups = defaultPGroupForLociNotUnderTest })
                .WithDonorType(DefaultDonorType)
                .Build();

            var donorHeterozygousForDifferentPGroups = new InputDonorBuilder(DonorIdGenerator.NextId())
                .WithHlaAtLocus(
                    locus,
                    new ExpandedHla { PGroups = nonMatchingPGroupAtPositionOne },
                    new ExpandedHla { PGroups = nonMatchingPGroupAtPositionTwo }
                )
                .WithDefaultRequiredHla(new ExpandedHla { PGroups = defaultPGroupForLociNotUnderTest })
                .WithDonorType(DefaultDonorType)
                .Build();

            twoOutOfTwoMatchCountDonors = new List<int>
            {
                donorHeterozygousForPatientPGroupAndNull.DonorId,
                donorHomozygousForPatientPGroup.DonorId
            };

            oneOutOfTwoMatchCountDonors = new List<int>
            {
                donorHeterozygousForPatientPGroupAndDifferentPGroup.DonorId
            };

            zeroOutOfTwoMatchCountDonors = new List<int>
            {
                donorHeterozygousForDifferentPGroupAndNull.DonorId,
                donorHomozygousForDifferentPGroup.DonorId,
                donorHeterozygousForDifferentPGroups.DonorId
            };

            var allDonors = new List<InputDonor>
            {
                donorHeterozygousForPatientPGroupAndNull,
                donorHomozygousForPatientPGroup,
                donorHeterozygousForPatientPGroupAndDifferentPGroup,
                donorHeterozygousForDifferentPGroupAndNull,
                donorHomozygousForDifferentPGroup,
                donorHeterozygousForDifferentPGroups
            };

            foreach (var donor in allDonors)
            {
                Task.Run(() => importRepo.AddOrUpdateDonorWithHla(donor)).Wait();
            }
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            ClearDatabase();
        }

        [Test]
        public async Task Search_HomozygousPatient_WithNoAllowedMismatches_MatchesTwoOutOfTwoMatchCountDonors()
        {
            var results = await matchingService.GetMatches(GetDefaultCriteriaBuilder().Build());

            results.Should().Contain(d => twoOutOfTwoMatchCountDonors.Contains(d.Donor.DonorId));
        }

        [Test]
        public async Task Search_HomozygousPatient_WithNoAllowedMismatches_DoesNotMatchLessThanTwoOutOfTwoMatchCountDonors()
        {
            var results = await matchingService.GetMatches(GetDefaultCriteriaBuilder().Build());

            var lessThanTwoOutOfTwoMatchCountDonors = oneOutOfTwoMatchCountDonors.Concat(zeroOutOfTwoMatchCountDonors);

            results.Should().Contain(d => lessThanTwoOutOfTwoMatchCountDonors.Contains(d.Donor.DonorId));
        }

        [Test]
        public async Task Search_HomozygousPatient_WithOneAllowedMismatchAtLocus_MatchesOneOutOfTwoMatchCountDonors()
        {
            const int mismatchCount = 1;
            var criteria = GetDefaultCriteriaBuilder()
                .WithTotalMismatchCount(mismatchCount)
                .WithLocusMatchCriteria(locus, GetPatientLocusMatchCriteria(mismatchCount))
                .Build();
            var results = await matchingService.GetMatches(criteria);

            results.Should().Contain(d => oneOutOfTwoMatchCountDonors.Contains(d.Donor.DonorId));
        }

        [Test]
        public async Task Search_HomozygousPatient_WithOneAllowedMismatchAtLocus_DoesNotMatchDonorsWithFewerOrGreaterThanOneMatchCountDonors()
        {
            const int mismatchCount = 1;
            var criteria = GetDefaultCriteriaBuilder()
                .WithTotalMismatchCount(mismatchCount)
                .WithLocusMatchCriteria(locus, GetPatientLocusMatchCriteria(mismatchCount))
                .Build();
            var results = await matchingService.GetMatches(criteria);

            var fewerOrGreaterThanOneMatchCountDonors = twoOutOfTwoMatchCountDonors.Concat(zeroOutOfTwoMatchCountDonors);

            results.Should().Contain(d => fewerOrGreaterThanOneMatchCountDonors.Contains(d.Donor.DonorId));
        }

        [Test]
        public async Task Search_HomozygousPatient_WithTwoAllowedMismatchesAtLocus_MatchesZeroOutOfTwoMatchCountDonors()
        {
            const int mismatchCount = 2;
            var criteria = GetDefaultCriteriaBuilder()
                .WithTotalMismatchCount(mismatchCount)
                .WithLocusMatchCriteria(locus, GetPatientLocusMatchCriteria(mismatchCount))
                .Build();
            var results = await matchingService.GetMatches(criteria);

            results.Should().Contain(d => zeroOutOfTwoMatchCountDonors.Contains(d.Donor.DonorId));
        }

        [Test]
        public async Task Search_HomozygousPatient_WithTwoAllowedMismatchesAtLocus_DoesNotMatchDonorsWithGreaterThanZeroMatchCountDonors()
        {
            const int mismatchCount = 2;
            var criteria = GetDefaultCriteriaBuilder()
                .WithTotalMismatchCount(mismatchCount)
                .WithLocusMatchCriteria(locus, GetPatientLocusMatchCriteria(mismatchCount))
                .Build();
            var results = await matchingService.GetMatches(criteria);

            var greaterThanZeroMatchCountDonors = twoOutOfTwoMatchCountDonors.Concat(oneOutOfTwoMatchCountDonors);

            results.Should().Contain(d => greaterThanZeroMatchCountDonors.Contains(d.Donor.DonorId));
        }

        /// <returns> A criteria builder pre-populated with default criteria data of an exact search.</returns>
        private AlleleLevelMatchCriteriaBuilder GetDefaultCriteriaBuilder()
        {
            return new AlleleLevelMatchCriteriaBuilder()
                .WithLocusMatchCriteria(locus, GetPatientLocusMatchCriteria(0))
                .WithDefaultLocusMatchCriteria(new AlleleLevelLocusMatchCriteria
                {
                    MismatchCount = 0,
                    PGroupsToMatchInPositionOne = defaultPGroupForLociNotUnderTest,
                    PGroupsToMatchInPositionTwo = defaultPGroupForLociNotUnderTest
                })
                .WithSearchType(DefaultDonorType)
                .WithTotalMismatchCount(0);
        }

        /// <summary>
        /// Patient P groups will be either set to homozygous by typing (same P group at both positions)
        /// or homozygous by expression (P group at one position and no P group at the other)
        /// depending on test fixture parameters.
        /// </summary>
        private AlleleLevelLocusMatchCriteria GetPatientLocusMatchCriteria(int mismatchCount)
        {
            return new AlleleLevelLocusMatchCriteria
            {
                MismatchCount = mismatchCount,
                PGroupsToMatchInPositionOne = patientPGroup,
                PGroupsToMatchInPositionTwo = patientHomozygousCategory == HomozygousBy.Typing
                    ? patientPGroup
                    : emptyPGroupForNullExpressingTyping
            };
        }
    }
}
