using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.GeneticData;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Test.SharedTestHelpers;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata;
using Atlas.MatchingAlgorithm.Client.Models.Donors;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.Data.Models.DonorInfo;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders.TransientSqlDatabase.RepositoryFactories;
using Atlas.MatchingAlgorithm.Services.Donors;
using Atlas.MatchingAlgorithm.Services.Search.Matching;
using Atlas.MatchingAlgorithm.Test.Integration.Resources.TestData;
using Atlas.MatchingAlgorithm.Test.Integration.TestHelpers;
using Atlas.MatchingAlgorithm.Test.Integration.TestHelpers.Builders;
using Atlas.MatchingAlgorithm.Test.Integration.TestHelpers.Repositories;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Integration.IntegrationTests.Matching
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
            public LocusInfo<string> ExpressedHlaTyping { get; }
            public string NullHlaTyping { get; }

            public LocusTypingInfo(Zygosity zygosity, LocusInfo<string> expressedHlaTyping, string nullHlaTyping = null)
            {
                Zygosity = zygosity;
                ExpressedHlaTyping = expressedHlaTyping;
                NullHlaTyping = nullHlaTyping;
            }

            public static LocusTypingInfo GetDefaultLocusConditions(LocusInfo<string> expressedHlaTyping)
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
        private LocusInfo<string> originalHlaAtLocusUnderTest;
        private LocusInfo<string> mismatchedHlaAtLocusUnderTest;
        private PhenotypeInfo<INullHandledHlaMatchingMetadata> patientMatchingHlaPhenotype;

        private IDonorHlaExpander donorHlaExpander;
        private IMatchingService matchingService;
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
            TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage(() =>
            {
                donorHlaExpander = DependencyInjection.DependencyInjection.Provider.GetService<IDonorHlaExpanderFactory>()
                    .BuildForActiveHlaNomenclatureVersion();
                criteriaFromExpandedHla = new AlleleLevelMatchCriteriaFromExpandedHla(LocusUnderTest, MatchingDonorType);

                SetSourceHlaPhenotypes();
                SetPatientMatchingHlaPhenotype();
                AddDonorsToRepository();
            });
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            TestStackTraceHelper.CatchAndRethrowWithStackTraceInExceptionMessage(DatabaseManager.ClearTransientDatabases);
        }

        [SetUp]
        public void SetUpBeforeEachTest()
        {
            DependencyInjection.DependencyInjection.NewScope();
            matchingService = DependencyInjection.DependencyInjection.Provider.GetService<IMatchingService>();
        }

        [Test]
        public async Task Search_WithNoAllowedMismatches_HomozygousPatient_MatchesTwoOutOfTwoMatchCountDonors()
        {
            var criteria = criteriaFromExpandedHla.GetAlleleLevelMatchCriteria(patientMatchingHlaPhenotype);
            var matchingDonors = await GetMatchingDonorIds(criteria);

            matchingDonors.Should().BeEquivalentTo(twoOutOfTwoMatchCountDonors);
        }

        [Test]
        public async Task Search_WithOneAllowedMismatchAtLocus_HomozygousPatient_MatchesOneOutOfTwoMatchCountDonors()
        {
            var criteria = criteriaFromExpandedHla.GetAlleleLevelMatchCriteria(patientMatchingHlaPhenotype, 1);
            var matchingDonors = await GetMatchingDonorIds(criteria);

            matchingDonors.Should().BeEquivalentTo(oneOutOfTwoMatchCountDonors);
        }

        [Test]
        public async Task Search_WithTwoAllowedMismatchesAtLocus_HomozygousPatient_MatchesZeroOutOfTwoMatchCountDonors()
        {
            var criteria = criteriaFromExpandedHla.GetAlleleLevelMatchCriteria(patientMatchingHlaPhenotype, 2);
            var matchingDonors = await GetMatchingDonorIds(criteria);

            matchingDonors.Should().BeEquivalentTo(zeroOutOfTwoMatchCountDonors);
        }

        private void SetSourceHlaPhenotypes()
        {
            originalHlaPhenotype = TestHlaPhenotypeSelector.GetTestHlaPhenotype(new SampleTestHlas.HeterozygousSet1(), patientTestCategory);
            var mismatchedHlaPhenotype = TestHlaPhenotypeSelector.GetTestHlaPhenotype(new SampleTestHlas.HeterozygousSet2(), patientTestCategory);

            originalHlaAtLocusUnderTest = originalHlaPhenotype.GetLocus(LocusUnderTest);
            mismatchedHlaAtLocusUnderTest = mismatchedHlaPhenotype.GetLocus(LocusUnderTest);
        }

        private void SetPatientMatchingHlaPhenotype()
        {
            var locusUnderTestConditions = new LocusTypingInfo(patientZygosity, originalHlaAtLocusUnderTest, OriginalNullAllele);

            var patientHlaPhenotype = GetHlaPhenotype(originalHlaPhenotype, locusUnderTestConditions);

            patientMatchingHlaPhenotype = donorHlaExpander.ExpandDonorHlaAsync(new DonorInfo {HlaNames = patientHlaPhenotype}).Result.MatchingHla;
        }

        private void AddDonorsToRepository()
        {
            var repositoryFactory = DependencyInjection.DependencyInjection.Provider.GetService<IActiveRepositoryFactory>();
            var donorRepo = repositoryFactory.GetDonorUpdateRepository();
            Task.Run(() => donorRepo.InsertBatchOfDonorsWithExpandedHla(BuildDonors(), false)).Wait();
        }

        private IEnumerable<DonorInfoWithExpandedHla> BuildDonors()
        {
            return
                BuildTwoOutOfTwoMatchCountDonors().Concat(
                    BuildOneOutOfTwoMatchCountDonors().Concat(
                        BuildZeroOutOfTwoMatchCountDonors()));
        }

        private IEnumerable<DonorInfoWithExpandedHla> BuildTwoOutOfTwoMatchCountDonors()
        {
            var donorWithOriginalHla1At1AndOriginalNullAt2 = new DonorInfoWithTestHlaBuilder(DonorIdGenerator.NextId())
                .WithHla(GetDonorMatchingHlaPhenotype(
                    new LocusTypingInfo(
                        Zygosity.HomozygousByExpression,
                        originalHlaAtLocusUnderTest,
                        OriginalNullAllele)))
                .Build();

            var donorWithOriginalHla1At1AndDifferentNullAt2 = new DonorInfoWithTestHlaBuilder(DonorIdGenerator.NextId())
                .WithHla(GetDonorMatchingHlaPhenotype(
                    new LocusTypingInfo(
                        Zygosity.HomozygousByExpression,
                        originalHlaAtLocusUnderTest,
                        DifferentNullAllele)))
                .Build();

            var donorHomozygousForOriginalHla1 = new DonorInfoWithTestHlaBuilder(DonorIdGenerator.NextId())
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
            var donorWithOriginalHla1At1AndOriginalHla2At2 = new DonorInfoWithTestHlaBuilder(DonorIdGenerator.NextId())
                .WithHla(GetDonorMatchingHlaPhenotype(
                    new LocusTypingInfo(
                        Zygosity.HeterozygousExpressing,
                        originalHlaAtLocusUnderTest)))
                .Build();

            var donorWithOriginalHla1At1AndMismatchedHla2At2 = new DonorInfoWithTestHlaBuilder(DonorIdGenerator.NextId())
                .WithHla(GetDonorMatchingHlaPhenotype(
                    new LocusTypingInfo(
                        Zygosity.HeterozygousExpressing,
                        originalHlaAtLocusUnderTest)))
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
            var donorWithMismatchedHla1At1AndOriginalHla2At2 = new DonorInfoWithTestHlaBuilder(DonorIdGenerator.NextId())
                .WithHla(GetDonorMatchingHlaPhenotype(
                    new LocusTypingInfo(
                        Zygosity.HeterozygousExpressing,
                        mismatchedHlaAtLocusUnderTest)))
                .Build();

            var donorWithMismatchedHla1At1AndOriginalNullAt2 = new DonorInfoWithTestHlaBuilder(DonorIdGenerator.NextId())
                .WithHla(GetDonorMatchingHlaPhenotype(
                    new LocusTypingInfo(
                        Zygosity.HomozygousByExpression,
                        mismatchedHlaAtLocusUnderTest,
                        OriginalNullAllele)))
                .Build();

            var donorWithMismatchedHla1At1AndDifferentNullAt2 = new DonorInfoWithTestHlaBuilder(DonorIdGenerator.NextId())
                .WithHla(GetDonorMatchingHlaPhenotype(
                    new LocusTypingInfo(
                        Zygosity.HomozygousByExpression,
                        mismatchedHlaAtLocusUnderTest,
                        DifferentNullAllele)))
                .Build();

            var donorHomozygousForMismatchedHla1 = new DonorInfoWithTestHlaBuilder(DonorIdGenerator.NextId())
                .WithHla(GetDonorMatchingHlaPhenotype(
                    new LocusTypingInfo(
                        Zygosity.HomozygousByTyping,
                        mismatchedHlaAtLocusUnderTest)))
                .Build();

            var donorWithMismatchedHla1At1AndMismatchedHla2At2 = new DonorInfoWithTestHlaBuilder(DonorIdGenerator.NextId())
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

        private PhenotypeInfo<INullHandledHlaMatchingMetadata> GetDonorMatchingHlaPhenotype(LocusTypingInfo locusUnderTestTypingInfo)
        {
            var donorHlaPhenotype = GetHlaPhenotype(originalHlaPhenotype, locusUnderTestTypingInfo);
            return donorHlaExpander.ExpandDonorHlaAsync(new DonorInfo {HlaNames = donorHlaPhenotype}).Result.MatchingHla;
        }

        private static PhenotypeInfo<string> GetHlaPhenotype(PhenotypeInfo<string> hlaPhenotype, LocusTypingInfo locusUnderTestTypingInfo)
        {
            return hlaPhenotype.MapByLocus((l, hla) =>
            {
                var locusConditions = l == LocusUnderTest
                    ? locusUnderTestTypingInfo
                    : LocusTypingInfo.GetDefaultLocusConditions(hla);
                var locusHlaTyping = GetLocusHlaTyping(locusConditions);
                return locusHlaTyping;
            });
        }

        /// <summary>
        /// Note: Position one of the expressing typing will be used to produce 
        /// the homozygous locus - whether it be homozygous by typing or expression.
        /// </summary>
        private static LocusInfo<string> GetLocusHlaTyping(LocusTypingInfo locusTypingInfo)
        {
            return locusTypingInfo.Zygosity switch
            {
                Zygosity.HomozygousByTyping => new LocusInfo<string>(locusTypingInfo.ExpressedHlaTyping.Position1),
                Zygosity.HeterozygousExpressing => locusTypingInfo.ExpressedHlaTyping,
                Zygosity.HomozygousByExpression when string.IsNullOrEmpty(locusTypingInfo.NullHlaTyping) =>
                throw new ArgumentException("Null HLA typing must be provided."),
                Zygosity.HomozygousByExpression => new LocusInfo<string>(locusTypingInfo.ExpressedHlaTyping.Position1, locusTypingInfo.NullHlaTyping),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        /// <summary>
        /// Runs the matching service based on match criteria.
        /// </summary>
        /// <returns>List of matching donor IDs.</returns>
        private async Task<IEnumerable<int>> GetMatchingDonorIds(AlleleLevelMatchCriteria alleleLevelMatchCriteria)
        {
            var results = await matchingService.GetMatches(alleleLevelMatchCriteria, null).ToListAsync();
            return results.Select(d => d.DonorId);
        }
    }
}