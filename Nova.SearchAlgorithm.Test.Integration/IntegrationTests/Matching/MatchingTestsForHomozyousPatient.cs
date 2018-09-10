using Autofac;
using FluentAssertions;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Repositories;
using Nova.SearchAlgorithm.Extensions;
using Nova.SearchAlgorithm.MatchingDictionary.Services;
using Nova.SearchAlgorithm.Services.Matching;
using Nova.SearchAlgorithm.Test.Integration.TestData;
using Nova.SearchAlgorithm.Test.Integration.TestHelpers.Builders;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nova.SearchAlgorithm.Test.Integration.IntegrationTests.Matching
{
    public enum Zygosity
    {
        HeterozygousExpressing,
        HomozygousByTyping,
        HomozygousByExpression
    }

    /// <summary>
    /// Check that the matching service returns expected donors for a patient
    /// that is either has the same expressed HLA at both positions (homozygous by typing)
    /// or that has one expressing typing and one unambiguously null typing (homozygous by expression).
    /// Only one locus is under test to keep things simple.
    /// Both patient and donor typings are based on the same source HLA phenotype.
    /// Expected match counts are determined by the decision to use position 1 of the source phenotype
    /// as the expressed typing within the homozygous patient locus.
    /// </summary>
    [TestFixture(Zygosity.HomozygousByTyping)]
    [TestFixture(Zygosity.HomozygousByExpression)]
    public class MatchingTestsForHomozygousPatient : IntegrationTestBase
    {
        private class LocusTypingInfo
        {
            public Zygosity Zygosity { get; }
            public Tuple<string, string> ExpressedHlaTyping { get; }
            public string NullHlaTyping { get; }

            public LocusTypingInfo(Zygosity zygosity, Tuple<string, string> expressedHlaTyping, string nullHlaTyping = null)
            {
                Zygosity = zygosity;
                ExpressedHlaTyping = expressedHlaTyping;
                NullHlaTyping = nullHlaTyping;
            }

            public static LocusTypingInfo GetDefaultLocusConditions(Tuple<string, string> expressedHlaTyping)
            {
                return new LocusTypingInfo(Zygosity.HeterozygousExpressing, expressedHlaTyping);
            }
        }

        private const Locus LocusUnderTest = Locus.A;
        private const string OriginalNullAllele = "02:15N";
        private const string DifferentNullAllele = "11:21N";

        private readonly Zygosity patientZygosity;

        private PhenotypeInfo<string> originalHlaPhenotype;
        private Tuple<string, string> originalHlaAtLocusUnderTest;
        private Tuple<string, string> mismatchedHlaAtLocusUnderTest;
        private PhenotypeInfo<ExpandedHla> patientMatchingHlaPhenotype;

        private ILocusHlaMatchingLookupService matchingHlaLookupService;
        private IDonorMatchingService donorMatchingService;

        private IEnumerable<int> twoOutOfTwoMatchCountDonors;
        private IEnumerable<int> oneOutOfTwoMatchCountDonors;
        private IEnumerable<int> zeroOutOfTwoMatchCountDonors;

        public MatchingTestsForHomozygousPatient(Zygosity patientZygosity)
        {
            this.patientZygosity = patientZygosity;
        }

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            matchingHlaLookupService = Container.Resolve<ILocusHlaMatchingLookupService>();

            SetSourceHlaPhenotypes();
            SetPatientMatchingHlaPhenotype();
            AddDonorsToRepository();
        }

        [SetUp]
        public void SetUpBeforeEachTest()
        {
            donorMatchingService = Container.Resolve<IDonorMatchingService>();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            ClearDatabase();
        }

        [Test]
        public async Task Search_HomozygousPatient_WithNoAllowedMismatches_MatchesTwoOutOfTwoMatchCountDonors()
        {
            var criteria = GetDefaultCriteriaBuilder();
            var matchingDonors = await GetMatchingDonorIds(criteria);

            matchingDonors.ShouldBeEquivalentTo(twoOutOfTwoMatchCountDonors);
        }

        [Test]
        public async Task Search_HomozygousPatient_WithOneAllowedMismatchAtLocus_MatchesOneOutOfTwoMatchCountDonors()
        {
            const int mismatchCount = 1;
            var criteria = GetDefaultCriteriaBuilder()
                .WithDonorMismatchCount(mismatchCount)
                .WithLocusMatchCriteria(LocusUnderTest, GetPatientLocusMatchCriteria(LocusUnderTest, mismatchCount));
            var matchingDonors = await GetMatchingDonorIds(criteria);

            matchingDonors.ShouldBeEquivalentTo(oneOutOfTwoMatchCountDonors);
        }

        [Test]
        public async Task Search_HomozygousPatient_WithTwoAllowedMismatchesAtLocus_MatchesZeroOutOfTwoMatchCountDonors()
        {
            const int mismatchCount = 2;
            var criteria = GetDefaultCriteriaBuilder()
                .WithDonorMismatchCount(mismatchCount)
                .WithLocusMatchCriteria(LocusUnderTest, GetPatientLocusMatchCriteria(LocusUnderTest, mismatchCount));
            var matchingDonors = await GetMatchingDonorIds(criteria);

            matchingDonors.ShouldBeEquivalentTo(zeroOutOfTwoMatchCountDonors);
        }

        private void SetSourceHlaPhenotypes()
        {
            originalHlaPhenotype = new TestHla.HeterozygousSet1().FiveLocus_SingleExpressingAlleles;
            var mismatchedHlaPhenotype = new TestHla.HeterozygousSet2().FiveLocus_SingleExpressingAlleles;

            originalHlaAtLocusUnderTest = originalHlaPhenotype.DataAtLocus(LocusUnderTest);
            mismatchedHlaAtLocusUnderTest = mismatchedHlaPhenotype.DataAtLocus(LocusUnderTest);
        }

        private void SetPatientMatchingHlaPhenotype()
        {
            var locusUnderTestConditions = new LocusTypingInfo(
                patientZygosity,
                originalHlaAtLocusUnderTest,
                OriginalNullAllele);

            var patientHlaPhenotype = GetHlaPhenotype(originalHlaPhenotype, locusUnderTestConditions);

            patientMatchingHlaPhenotype = patientHlaPhenotype.ToExpandedHlaPhenotype(matchingHlaLookupService).Result;
        }

        private void AddDonorsToRepository()
        {
            var importRepo = Container.Resolve<IDonorImportRepository>();
            foreach (var donor in BuildInputDonors())
            {
                Task.Run(() => importRepo.AddOrUpdateDonorWithHla(donor)).Wait();
            }
        }

        private IEnumerable<InputDonor> BuildInputDonors()
        {
            return
                BuildTwoOutOfTwoMatchCountDonors().Concat(
                    BuildOneOutOfTwoMatchCountDonors().Concat(
                        BuildZeroOutOfTwoMatchCountDonors()));
        }

        private IEnumerable<InputDonor> BuildTwoOutOfTwoMatchCountDonors()
        {
            var donorWithOriginalHla1At1AndOriginalNullAt2 = new InputDonorBuilder(DonorIdGenerator.NextId())
                .WithMatchingHla(GetDonorMatchingHlaPhenotype(
                    new LocusTypingInfo(
                        Zygosity.HomozygousByExpression,
                        originalHlaAtLocusUnderTest,
                        OriginalNullAllele)))
                .Build();

            var donorWithOriginalHla1At1AndDifferentNullAt2 = new InputDonorBuilder(DonorIdGenerator.NextId())
                .WithMatchingHla(GetDonorMatchingHlaPhenotype(
                    new LocusTypingInfo(
                        Zygosity.HomozygousByExpression,
                        originalHlaAtLocusUnderTest,
                        DifferentNullAllele)))
                .Build();

            var donorHomozygousForOriginalHla1 = new InputDonorBuilder(DonorIdGenerator.NextId())
                .WithMatchingHla(GetDonorMatchingHlaPhenotype(
                    new LocusTypingInfo(
                        Zygosity.HomozygousByTyping,
                        originalHlaAtLocusUnderTest)))
                .Build();

            var donors = new List<InputDonor>
            {
                donorWithOriginalHla1At1AndOriginalNullAt2,
                donorWithOriginalHla1At1AndDifferentNullAt2,
                donorHomozygousForOriginalHla1
            };

            twoOutOfTwoMatchCountDonors = donors.Select(d => d.DonorId);

            return donors;
        }

        private IEnumerable<InputDonor> BuildOneOutOfTwoMatchCountDonors()
        {
            var donorWithOriginalHla1At1AndOriginalHla2At2 = new InputDonorBuilder(DonorIdGenerator.NextId())
                .WithMatchingHla(GetDonorMatchingHlaPhenotype(
                    new LocusTypingInfo(
                        Zygosity.HeterozygousExpressing,
                        originalHlaAtLocusUnderTest)))
                .Build();

            var donorWithOriginalHla1At1AndMismatchedHla2At2 = new InputDonorBuilder(DonorIdGenerator.NextId())
                .WithMatchingHla(GetDonorMatchingHlaPhenotype(
                    new LocusTypingInfo(
                        Zygosity.HeterozygousExpressing,
                        new Tuple<string, string>(originalHlaAtLocusUnderTest.Item1, mismatchedHlaAtLocusUnderTest.Item2))))
                .Build();

            var donors = new List<InputDonor>
            {
                donorWithOriginalHla1At1AndOriginalHla2At2,
                donorWithOriginalHla1At1AndMismatchedHla2At2
            };

            oneOutOfTwoMatchCountDonors = donors.Select(d => d.DonorId);

            return donors;
        }

        private IEnumerable<InputDonor> BuildZeroOutOfTwoMatchCountDonors()
        {
            var donorWithMismatchedHla1At1AndOriginalHla2At2 = new InputDonorBuilder(DonorIdGenerator.NextId())
                .WithMatchingHla(GetDonorMatchingHlaPhenotype(
                    new LocusTypingInfo(
                        Zygosity.HeterozygousExpressing,
                        new Tuple<string, string>(mismatchedHlaAtLocusUnderTest.Item1, originalHlaAtLocusUnderTest.Item2))))
                .Build();

            var donorWithMismatchedHla1At1AndOriginalNullAt2 = new InputDonorBuilder(DonorIdGenerator.NextId())
                .WithMatchingHla(GetDonorMatchingHlaPhenotype(
                    new LocusTypingInfo(
                        Zygosity.HomozygousByExpression,
                        mismatchedHlaAtLocusUnderTest,
                        OriginalNullAllele)))
                .Build();

            var donorWithMismatchedHla1At1AndDifferentNullAt2 = new InputDonorBuilder(DonorIdGenerator.NextId())
                .WithMatchingHla(GetDonorMatchingHlaPhenotype(
                    new LocusTypingInfo(
                        Zygosity.HomozygousByExpression,
                        mismatchedHlaAtLocusUnderTest,
                        DifferentNullAllele)))
                .Build();

            var donorHomozygousForMismatchedHla1 = new InputDonorBuilder(DonorIdGenerator.NextId())
                .WithMatchingHla(GetDonorMatchingHlaPhenotype(
                    new LocusTypingInfo(
                        Zygosity.HomozygousByTyping,
                        mismatchedHlaAtLocusUnderTest)))
                .Build();

            var donorWithMismatchedHla1At1AndMismatchedHla2At2 = new InputDonorBuilder(DonorIdGenerator.NextId())
                .WithMatchingHla(GetDonorMatchingHlaPhenotype(
                    new LocusTypingInfo(
                        Zygosity.HeterozygousExpressing,
                        mismatchedHlaAtLocusUnderTest)))
                .Build();

            var donors = new List<InputDonor>
            {
                donorWithMismatchedHla1At1AndOriginalHla2At2,
                donorWithMismatchedHla1At1AndOriginalNullAt2,
                donorWithMismatchedHla1At1AndDifferentNullAt2,
                donorHomozygousForMismatchedHla1,
                donorWithMismatchedHla1At1AndMismatchedHla2At2
            };

            zeroOutOfTwoMatchCountDonors = donors.Select(d => d.DonorId);

            return donors;
        }

        private PhenotypeInfo<ExpandedHla> GetDonorMatchingHlaPhenotype(LocusTypingInfo locusUnderTestTypingInfo)
        {
            var donorHlaPhenotype = GetHlaPhenotype(originalHlaPhenotype, locusUnderTestTypingInfo);
            return donorHlaPhenotype.ToExpandedHlaPhenotype(matchingHlaLookupService).Result;
        }

        private static PhenotypeInfo<string> GetHlaPhenotype(
            PhenotypeInfo<string> hlaPhenotype,
            LocusTypingInfo locusUnderTestTypingInfo)
        {
            return hlaPhenotype.MapByLocus((l, hla1, hla2) =>
            {
                var locusConditions = l == LocusUnderTest
                    ? locusUnderTestTypingInfo
                    : LocusTypingInfo.GetDefaultLocusConditions(new Tuple<string, string>(hla1, hla2));
                return GetLocusHlaTyping(locusConditions);
            });
        }

        /// <summary>
        /// Note: Position one of the expressing typing will be used to produce 
        /// the homozygous locus - whether it be homozygous by typing or expression.
        /// </summary>
        private static Tuple<string, string> GetLocusHlaTyping(LocusTypingInfo locusTypingInfo)
        {
            switch (locusTypingInfo.Zygosity)
            {
                case Zygosity.HomozygousByTyping:
                    return new Tuple<string, string>(locusTypingInfo.ExpressedHlaTyping.Item1, locusTypingInfo.ExpressedHlaTyping.Item1);
                case Zygosity.HomozygousByExpression:
                    return string.IsNullOrEmpty(locusTypingInfo.NullHlaTyping)
                        ? throw new ArgumentException("Null HLA typing must be provided.")
                        : new Tuple<string, string>(locusTypingInfo.ExpressedHlaTyping.Item1, locusTypingInfo.NullHlaTyping);
                case Zygosity.HeterozygousExpressing:
                    return locusTypingInfo.ExpressedHlaTyping;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <returns> A criteria builder pre-populated with default criteria data of an exact search.</returns>
        private AlleleLevelMatchCriteriaBuilder GetDefaultCriteriaBuilder()
        {
            const int mismatchCount = 0;
            return new AlleleLevelMatchCriteriaBuilder()
                .WithDonorMismatchCount(mismatchCount)
                .WithLocusMatchCriteria(Locus.A, GetPatientLocusMatchCriteria(Locus.A, mismatchCount))
                .WithLocusMatchCriteria(Locus.B, GetPatientLocusMatchCriteria(Locus.B, mismatchCount))
                .WithLocusMatchCriteria(Locus.Drb1, GetPatientLocusMatchCriteria(Locus.Drb1, mismatchCount));
        }

        private AlleleLevelLocusMatchCriteria GetPatientLocusMatchCriteria(
            Locus locus,
            int mismatchCount)
        {
            return new AlleleLevelLocusMatchCriteria
            {
                MismatchCount = mismatchCount,
                PGroupsToMatchInPositionOne = GetOriginalHlasAt(locus, TypePositions.One),
                PGroupsToMatchInPositionTwo = GetOriginalHlasAt(locus, TypePositions.Two)
            };
        }

        private IEnumerable<string> GetOriginalHlasAt(Locus locus, TypePositions typePosition)
        {
            return patientMatchingHlaPhenotype.DataAtPosition(locus, typePosition).PGroups;
        }

        /// <summary>
        /// Runs the matching service based on match criteria.
        /// </summary>
        /// <returns>List of matching donor IDs.</returns>
        private async Task<IEnumerable<int>> GetMatchingDonorIds(AlleleLevelMatchCriteriaBuilder alleleLevelMatchCriteriaBuilder)
        {
            var results = await donorMatchingService.GetMatches(alleleLevelMatchCriteriaBuilder.Build());
            return results.Select(d => d.DonorId);
        }
    }
}
