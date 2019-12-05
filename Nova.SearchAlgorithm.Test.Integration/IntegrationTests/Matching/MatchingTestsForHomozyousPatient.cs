using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Data.Models.DonorInfo;
using Nova.SearchAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Nova.SearchAlgorithm.Services.MatchingDictionary;
using Nova.SearchAlgorithm.Services.Search.Matching;
using Nova.SearchAlgorithm.Test.Integration.TestData;
using Nova.SearchAlgorithm.Test.Integration.TestHelpers;
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
    /// that either has the same expressed HLA at both positions (homozygous by typing)
    /// or that has one expressing typing and one unambiguously null typing (homozygous by expression).
    /// Only one locus is under test to keep things simple.
    /// Both patient and donor typings are based on the same source HLA phenotype.
    /// Expected match counts are determined by the decision to use position 1 of the source phenotype
    /// as the expressed typing within the homozygous patient locus.
    /// </summary>
    [TestFixture(Zygosity.HomozygousByTyping, TestHlaPhenotypeCategory.SixLocusSingleExpressingAlleles)]
    [TestFixture(Zygosity.HomozygousByTyping, TestHlaPhenotypeCategory.SixLocusExpressingAllelesWithTruncatedNames)]
    [TestFixture(Zygosity.HomozygousByTyping, TestHlaPhenotypeCategory.SixLocusXxCodes)]
    [TestFixture(Zygosity.HomozygousByTyping, TestHlaPhenotypeCategory.FiveLocusSerologies)]
    [TestFixture(Zygosity.HomozygousByExpression, TestHlaPhenotypeCategory.SixLocusSingleExpressingAlleles)]
    [TestFixture(Zygosity.HomozygousByExpression, TestHlaPhenotypeCategory.SixLocusExpressingAllelesWithTruncatedNames)]
    [TestFixture(Zygosity.HomozygousByExpression, TestHlaPhenotypeCategory.SixLocusXxCodes)]
    [TestFixture(Zygosity.HomozygousByExpression, TestHlaPhenotypeCategory.FiveLocusSerologies)]
    public class MatchingTestsForHomozygousPatient
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
        private const DonorType MatchingDonorType = DonorType.Adult;
        private const string OriginalNullAllele = "02:15N";
        private const string DifferentNullAllele = "11:21N";

        private readonly Zygosity patientZygosity;
        private readonly TestHlaPhenotypeCategory patientTestCategory;

        private PhenotypeInfo<string> originalHlaPhenotype;
        private Tuple<string, string> originalHlaAtLocusUnderTest;
        private Tuple<string, string> mismatchedHlaAtLocusUnderTest;
        private PhenotypeInfo<ExpandedHla> patientMatchingHlaPhenotype;

        private IExpandHlaPhenotypeService expandHlaPhenotypeService;
        private IDonorMatchingService donorMatchingService;
        private AlleleLevelMatchCriteriaFromExpandedHla criteriaFromExpandedHla;

        private IEnumerable<int> twoOutOfTwoMatchCountDonors;
        private IEnumerable<int> oneOutOfTwoMatchCountDonors;
        private IEnumerable<int> zeroOutOfTwoMatchCountDonors;

        public MatchingTestsForHomozygousPatient(
            Zygosity patientZygosity,
            TestHlaPhenotypeCategory patientTestCategory)
        {
            this.patientZygosity = patientZygosity;
            this.patientTestCategory = patientTestCategory;
        }

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            expandHlaPhenotypeService = DependencyInjection.DependencyInjection.Provider.GetService<IExpandHlaPhenotypeService>();
            criteriaFromExpandedHla = new AlleleLevelMatchCriteriaFromExpandedHla(
                LocusUnderTest,
                MatchingDonorType);

            SetSourceHlaPhenotypes();
            SetPatientMatchingHlaPhenotype();
            AddDonorsToRepository();
        }
        
        [OneTimeTearDown]
        public void TearDown()
        {
            DatabaseManager.ClearDatabases();
        }

        [SetUp]
        public void SetUpBeforeEachTest()
        {
            donorMatchingService = DependencyInjection.DependencyInjection.Provider.GetService<IDonorMatchingService>();
        }

        [Test]
        public async Task Search_WithNoAllowedMismatches_HomozygousPatient_MatchesTwoOutOfTwoMatchCountDonors()
        {
            var criteria = criteriaFromExpandedHla.GetAlleleLevelMatchCriteria(patientMatchingHlaPhenotype);
            var matchingDonors = await GetMatchingDonorIds(criteria);

            matchingDonors.ShouldBeEquivalentTo(twoOutOfTwoMatchCountDonors);
        }

        [Test]
        public async Task Search_WithOneAllowedMismatchAtLocus_HomozygousPatient_MatchesOneOutOfTwoMatchCountDonors()
        {
            var criteria = criteriaFromExpandedHla.GetAlleleLevelMatchCriteria(patientMatchingHlaPhenotype, 1);
            var matchingDonors = await GetMatchingDonorIds(criteria);

            matchingDonors.ShouldBeEquivalentTo(oneOutOfTwoMatchCountDonors);
        }

        [Test]
        public async Task Search_WithTwoAllowedMismatchesAtLocus_HomozygousPatient_MatchesZeroOutOfTwoMatchCountDonors()
        {
            var criteria = criteriaFromExpandedHla.GetAlleleLevelMatchCriteria(patientMatchingHlaPhenotype, 2);
            var matchingDonors = await GetMatchingDonorIds(criteria);

            matchingDonors.ShouldBeEquivalentTo(zeroOutOfTwoMatchCountDonors);
        }

        private void SetSourceHlaPhenotypes()
        {
            originalHlaPhenotype = TestHlaPhenotypeSelector.GetTestHlaPhenotype(new TestHla.HeterozygousSet1(), patientTestCategory);
            var mismatchedHlaPhenotype = TestHlaPhenotypeSelector.GetTestHlaPhenotype(new TestHla.HeterozygousSet2(), patientTestCategory);

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

            patientMatchingHlaPhenotype = expandHlaPhenotypeService.GetPhenotypeOfExpandedHla(patientHlaPhenotype).Result;
        }

        private void AddDonorsToRepository()
        {
            var repositoryFactory = DependencyInjection.DependencyInjection.Provider.GetService<IActiveRepositoryFactory>();
            var donorRepo = repositoryFactory.GetDonorUpdateRepository();
            Task.Run(() => donorRepo.InsertBatchOfDonorsWithExpandedHla(BuildInputDonors())).Wait();
        }

        private IEnumerable<DonorInfoWithExpandedHla> BuildInputDonors()
        {
            return
                BuildTwoOutOfTwoMatchCountDonors().Concat(
                    BuildOneOutOfTwoMatchCountDonors().Concat(
                        BuildZeroOutOfTwoMatchCountDonors()));
        }

        private IEnumerable<DonorInfoWithExpandedHla> BuildTwoOutOfTwoMatchCountDonors()
        {
            var donorWithOriginalHla1At1AndOriginalNullAt2 = new DonorInfoWithExpandedHlaBuilder(DonorIdGenerator.NextId())
                .WithHla(GetDonorMatchingHlaPhenotype(
                    new LocusTypingInfo(
                        Zygosity.HomozygousByExpression,
                        originalHlaAtLocusUnderTest,
                        OriginalNullAllele)))
                .Build();

            var donorWithOriginalHla1At1AndDifferentNullAt2 = new DonorInfoWithExpandedHlaBuilder(DonorIdGenerator.NextId())
                .WithHla(GetDonorMatchingHlaPhenotype(
                    new LocusTypingInfo(
                        Zygosity.HomozygousByExpression,
                        originalHlaAtLocusUnderTest,
                        DifferentNullAllele)))
                .Build();

            var donorHomozygousForOriginalHla1 = new DonorInfoWithExpandedHlaBuilder(DonorIdGenerator.NextId())
                .WithHla(GetDonorMatchingHlaPhenotype(
                    new LocusTypingInfo(
                        Zygosity.HomozygousByTyping,
                        originalHlaAtLocusUnderTest)))
                .Build();

            var donors = new List<DonorInfoWithExpandedHla>
            {
                donorWithOriginalHla1At1AndOriginalNullAt2,
                donorWithOriginalHla1At1AndDifferentNullAt2,
                donorHomozygousForOriginalHla1
            };

            twoOutOfTwoMatchCountDonors = donors.Select(d => d.DonorId);

            return donors;
        }

        private IEnumerable<DonorInfoWithExpandedHla> BuildOneOutOfTwoMatchCountDonors()
        {
            var donorWithOriginalHla1At1AndOriginalHla2At2 = new DonorInfoWithExpandedHlaBuilder(DonorIdGenerator.NextId())
                .WithHla(GetDonorMatchingHlaPhenotype(
                    new LocusTypingInfo(
                        Zygosity.HeterozygousExpressing,
                        originalHlaAtLocusUnderTest)))
                .Build();

            var donorWithOriginalHla1At1AndMismatchedHla2At2 = new DonorInfoWithExpandedHlaBuilder(DonorIdGenerator.NextId())
                .WithHla(GetDonorMatchingHlaPhenotype(
                    new LocusTypingInfo(
                        Zygosity.HeterozygousExpressing,
                        new Tuple<string, string>(originalHlaAtLocusUnderTest.Item1, mismatchedHlaAtLocusUnderTest.Item2))))
                .Build();

            var donors = new List<DonorInfoWithExpandedHla>
            {
                donorWithOriginalHla1At1AndOriginalHla2At2,
                donorWithOriginalHla1At1AndMismatchedHla2At2
            };

            oneOutOfTwoMatchCountDonors = donors.Select(d => d.DonorId);

            return donors;
        }

        private IEnumerable<DonorInfoWithExpandedHla> BuildZeroOutOfTwoMatchCountDonors()
        {
            var donorWithMismatchedHla1At1AndOriginalHla2At2 = new DonorInfoWithExpandedHlaBuilder(DonorIdGenerator.NextId())
                .WithHla(GetDonorMatchingHlaPhenotype(
                    new LocusTypingInfo(
                        Zygosity.HeterozygousExpressing,
                        new Tuple<string, string>(mismatchedHlaAtLocusUnderTest.Item1, originalHlaAtLocusUnderTest.Item2))))
                .Build();

            var donorWithMismatchedHla1At1AndOriginalNullAt2 = new DonorInfoWithExpandedHlaBuilder(DonorIdGenerator.NextId())
                .WithHla(GetDonorMatchingHlaPhenotype(
                    new LocusTypingInfo(
                        Zygosity.HomozygousByExpression,
                        mismatchedHlaAtLocusUnderTest,
                        OriginalNullAllele)))
                .Build();

            var donorWithMismatchedHla1At1AndDifferentNullAt2 = new DonorInfoWithExpandedHlaBuilder(DonorIdGenerator.NextId())
                .WithHla(GetDonorMatchingHlaPhenotype(
                    new LocusTypingInfo(
                        Zygosity.HomozygousByExpression,
                        mismatchedHlaAtLocusUnderTest,
                        DifferentNullAllele)))
                .Build();

            var donorHomozygousForMismatchedHla1 = new DonorInfoWithExpandedHlaBuilder(DonorIdGenerator.NextId())
                .WithHla(GetDonorMatchingHlaPhenotype(
                    new LocusTypingInfo(
                        Zygosity.HomozygousByTyping,
                        mismatchedHlaAtLocusUnderTest)))
                .Build();

            var donorWithMismatchedHla1At1AndMismatchedHla2At2 = new DonorInfoWithExpandedHlaBuilder(DonorIdGenerator.NextId())
                .WithHla(GetDonorMatchingHlaPhenotype(
                    new LocusTypingInfo(
                        Zygosity.HeterozygousExpressing,
                        mismatchedHlaAtLocusUnderTest)))
                .Build();

            var donors = new List<DonorInfoWithExpandedHla>
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
            return expandHlaPhenotypeService.GetPhenotypeOfExpandedHla(donorHlaPhenotype).Result;
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

        /// <summary>
        /// Runs the matching service based on match criteria.
        /// </summary>
        /// <returns>List of matching donor IDs.</returns>
        private async Task<IEnumerable<int>> GetMatchingDonorIds(AlleleLevelMatchCriteria alleleLevelMatchCriteria)
        {
            var results = await donorMatchingService.GetMatches(alleleLevelMatchCriteria);
            return results.Select(d => d.DonorId);
        }
    }
}
