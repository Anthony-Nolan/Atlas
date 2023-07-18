using Atlas.Common.Caching;
using Atlas.Common.Test.SharedTestHelpers.Builders;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.HLATypings;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata.ScoringMetadata;
using Atlas.HlaMetadataDictionary.Test.TestHelpers.Builders;
using Atlas.HlaMetadataDictionary.Test.TestHelpers.Builders.ScoringInfoBuilders;
using Atlas.MatchingAlgorithm.Common.Models.Scoring;
using Atlas.MatchingAlgorithm.Services.ConfigurationProviders;
using Atlas.MatchingAlgorithm.Services.Search.Scoring;
using Atlas.MatchingAlgorithm.Services.Search.Scoring.Grading;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using System;
using Atlas.Client.Models.Search.Results.Matching.PerLocus;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;

namespace Atlas.MatchingAlgorithm.Test.Services.Search.Scoring.Grading
{
    public class GradingServiceTests
    {
        private const Locus MatchedLocus = Locus.A;
        private IGradingService gradingService;
        private IHlaScoringMetadata defaultSerologyResult;

        [SetUp]
        public void SetUpBeforeEachTest()
        {
            var scoringCache = new ScoringCache(
                new PersistentCacheProvider(AppCacheBuilder.NewDefaultCache()),
                Substitute.For<IActiveHlaNomenclatureVersionAccessor>());

            gradingService = new GradingService(scoringCache);

            defaultSerologyResult =
                new HlaScoringMetadataBuilder()
                    .AtLocus(MatchedLocus)
                    .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                        .WithMatchingSerologies(new[]
                        {
                            new SerologyEntry("default-serology", SerologySubtype.NotSplit, true)
                        })
                        .Build())
                    .Build();
        }

        #region Tests: Exception Cases

        [Test]
        public void CalculateGrades_PatientPhenotypeIsNull_ThrowsException()
        {
            var donorPhenotype = new PhenotypeInfo<IHlaScoringMetadata>();

            Assert.Throws<ArgumentException>(() => gradingService.CalculateGrades(null, donorPhenotype));
        }

        [Test]
        public void CalculateGrades_DonorPhenotypeIsNull_ThrowsException()
        {
            var patientPhenotype = new PhenotypeInfo<IHlaScoringMetadata>();

            Assert.Throws<ArgumentException>(() => gradingService.CalculateGrades(patientPhenotype, null));
        }

        [Test]
        public void CalculateGrades_BothPhenotypesAreNull_ThrowsException()
        {
            Assert.Throws<ArgumentException>(() => gradingService.CalculateGrades(null, null));
        }

        #endregion

        #region Tests: Best Grades & Orientation(s) returned

        [Test]
        public void CalculateGrades_TwoMatchesInDirect_TwoMismatchesInCross_ReturnsTwoMatchGradesInDirectOrientation()
        {
            const string sharedGGroup1 = "g-group-1";
            const string sharedGGroup2 = "g-group-2";

            var patientResult1 = new HlaScoringMetadataBuilder()
                .AtLocus(MatchedLocus)
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithMatchingGGroup(sharedGGroup1)
                    .Build())
                .Build();

            var patientResult2 = new HlaScoringMetadataBuilder()
                .AtLocus(MatchedLocus)
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithMatchingGGroup(sharedGGroup2)
                    .Build())
                .Build();

            var donorResult1 = new HlaScoringMetadataBuilder()
                .AtLocus(MatchedLocus)
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithMatchingGGroup(sharedGGroup1)
                    .Build())
                .Build();

            var donorResult2 = new HlaScoringMetadataBuilder()
                .AtLocus(MatchedLocus)
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithMatchingGGroup(sharedGGroup2)
                    .Build())
                .Build();

            var patientLookupResults = BuildMetadataAtMatchedLocus(patientResult1, patientResult2);
            var donorLookupResults = BuildMetadataAtMatchedLocus(donorResult1, donorResult2);
            var actualGradingResults = gradingService.CalculateGrades(patientLookupResults, donorLookupResults);

            var expectedMatchOrientations = new[] {MatchOrientation.Direct};
            var expectedGradingResult1 = new MatchGradeResult(MatchGrade.GGroup, expectedMatchOrientations);
            var expectedGradingResult2 = new MatchGradeResult(MatchGrade.GGroup, expectedMatchOrientations);

            // Direct grade (P1: D1) is GGroup; Cross (P1: D2) is Mismatch
            actualGradingResults.A.Position1.Should().BeEquivalentTo(expectedGradingResult1);
            // Direct grade (P2: D2) is GGroup; Cross (P2: D1) is Mismatch
            actualGradingResults.A.Position2.Should().BeEquivalentTo(expectedGradingResult2);
        }

        [Test]
        public void CalculateGrades_TwoBetterMatchesInDirect_TwoWorseMatchesInCross_ReturnsTwoBetterMatchGradesInDirectOrientation()
        {
            const string sharedAlleleName1 = "111:111";
            const string sharedAlleleName2 = "999:999";
            const string sharedPGroup = "shared-p-group";
            var gDnaStatus = new AlleleTypingStatus(SequenceStatus.Full, DnaCategory.GDna);
            var cDnaStatus = new AlleleTypingStatus(SequenceStatus.Full, DnaCategory.CDna);

            var patientResult1 = new HlaScoringMetadataBuilder()
                .AtLocus(MatchedLocus)
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithAlleleName(sharedAlleleName1)
                    .WithAlleleTypingStatus(gDnaStatus)
                    .WithMatchingPGroup(sharedPGroup)
                    .Build())
                .Build();

            var patientResult2 = new HlaScoringMetadataBuilder()
                .AtLocus(MatchedLocus)
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithAlleleName(sharedAlleleName2)
                    .WithAlleleTypingStatus(cDnaStatus)
                    .WithMatchingPGroup(sharedPGroup)
                    .Build())
                .Build();

            var donorResult1 = new HlaScoringMetadataBuilder()
                .AtLocus(MatchedLocus)
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithAlleleName(sharedAlleleName1)
                    .WithAlleleTypingStatus(gDnaStatus)
                    .WithMatchingPGroup(sharedPGroup)
                    .Build())
                .Build();

            var donorResult2 = new HlaScoringMetadataBuilder()
                .AtLocus(MatchedLocus)
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithAlleleName(sharedAlleleName2)
                    .WithAlleleTypingStatus(cDnaStatus)
                    .WithMatchingPGroup(sharedPGroup)
                    .Build())
                .Build();

            var patientLookupResults = BuildMetadataAtMatchedLocus(patientResult1, patientResult2);
            var donorLookupResults = BuildMetadataAtMatchedLocus(donorResult1, donorResult2);
            var actualGradingResults = gradingService.CalculateGrades(patientLookupResults, donorLookupResults);

            var expectedMatchOrientations = new[] {MatchOrientation.Direct};
            var expectedGradingResult1 = new MatchGradeResult(MatchGrade.GDna, expectedMatchOrientations);
            var expectedGradingResult2 = new MatchGradeResult(MatchGrade.CDna, expectedMatchOrientations);

            // Direct grade (P1: D1) is GDna; Cross (P1: D2) is PGroup
            actualGradingResults.A.Position1.Should().BeEquivalentTo(expectedGradingResult1);
            // Direct grade (P2: D2) is CDna; Cross (P2: D1) is PGroup
            actualGradingResults.A.Position2.Should().BeEquivalentTo(expectedGradingResult2);
        }

        [Test]
        public void CalculateGrades_BetterMatchWithMismatchInDirect_WorseMatchWithMismatchInCross_ReturnsBetterMatchWithMismatchInDirectOrientation()
        {
            const string matchingAssociatedName = "associated";
            const string matchingSplitName = "matching-split";
            var betterMatchingSerologies = new[]
            {
                new SerologyEntry(matchingAssociatedName, SerologySubtype.Associated, true),
                new SerologyEntry(matchingSplitName, SerologySubtype.Split, false)
            };

            var patientResult1 = new HlaScoringMetadataBuilder()
                .AtLocus(MatchedLocus)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithMatchingSerologies(betterMatchingSerologies)
                    .Build())
                .Build();

            var patientResult2 = new HlaScoringMetadataBuilder()
                .AtLocus(MatchedLocus)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithMatchingSerologies(new[] {new SerologyEntry("mismatched-not-split", SerologySubtype.NotSplit, true)})
                    .Build())
                .Build();

            var donorResult1 = new HlaScoringMetadataBuilder()
                .AtLocus(MatchedLocus)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithMatchingSerologies(betterMatchingSerologies)
                    .Build())
                .Build();

            var donorResult2 = new HlaScoringMetadataBuilder()
                .AtLocus(MatchedLocus)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithMatchingSerologies(new[]
                    {
                        new SerologyEntry(matchingSplitName, SerologySubtype.Split, true),
                        new SerologyEntry(matchingAssociatedName, SerologySubtype.Associated, false)
                    })
                    .Build())
                .Build();

            var patientLookupResults = BuildMetadataAtMatchedLocus(patientResult1, patientResult2);
            var donorLookupResults = BuildMetadataAtMatchedLocus(donorResult1, donorResult2);
            var actualGradingResults = gradingService.CalculateGrades(patientLookupResults, donorLookupResults);

            var expectedMatchOrientations = new[] {MatchOrientation.Direct};
            var expectedGradingResult1 = new MatchGradeResult(MatchGrade.Associated, expectedMatchOrientations);
            var expectedGradingResult2 = new MatchGradeResult(MatchGrade.Mismatch, expectedMatchOrientations);

            // Direct grade (P1: D1) is Associated; Cross (P1: D2) is Split
            actualGradingResults.A.Position1.Should().BeEquivalentTo(expectedGradingResult1);
            // Direct grade (P2: D2) is Mismatch; Cross (P2: D1) is Mismatch
            actualGradingResults.A.Position2.Should().BeEquivalentTo(expectedGradingResult2);
        }

        [Test]
        public void CalculateGrades_TwoMismatchesInDirect_TwoMatchesInCross_ReturnsTwoMatchesInCrossOrientation()
        {
            const string sharedGGroup1 = "g-group-1";
            const string sharedGGroup2 = "g-group-2";

            var patientResult1 = new HlaScoringMetadataBuilder()
                .AtLocus(MatchedLocus)
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithMatchingGGroup(sharedGGroup1)
                    .Build())
                .Build();

            var patientResult2 = new HlaScoringMetadataBuilder()
                .AtLocus(MatchedLocus)
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithMatchingGGroup(sharedGGroup2)
                    .Build())
                .Build();

            var donorResult1 = new HlaScoringMetadataBuilder()
                .AtLocus(MatchedLocus)
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithMatchingGGroup(sharedGGroup2)
                    .Build())
                .Build();

            var donorResult2 = new HlaScoringMetadataBuilder()
                .AtLocus(MatchedLocus)
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithMatchingGGroup(sharedGGroup1)
                    .Build())
                .Build();

            var patientLookupResults = BuildMetadataAtMatchedLocus(patientResult1, patientResult2);
            var donorLookupResults = BuildMetadataAtMatchedLocus(donorResult1, donorResult2);
            var actualGradingResults = gradingService.CalculateGrades(patientLookupResults, donorLookupResults);

            var expectedMatchOrientations = new[] {MatchOrientation.Cross};
            var expectedGradingResult1 = new MatchGradeResult(MatchGrade.GGroup, expectedMatchOrientations);
            var expectedGradingResult2 = new MatchGradeResult(MatchGrade.GGroup, expectedMatchOrientations);

            // Direct grade (P1: D1) is Mismatch; Cross (P1: D2) is GGroup
            actualGradingResults.A.Position1.Should().BeEquivalentTo(expectedGradingResult1);
            // Direct grade (P2: D2) is Mismatch; Cross (P2: D1) is GGroup
            actualGradingResults.A.Position2.Should().BeEquivalentTo(expectedGradingResult2);
        }

        [Test]
        public void CalculateGrades_TwoWorseMatchesInDirect_TwoBetterMatchesInCross_ReturnsTwoBetterMatchesInCrossOrientation()
        {
            const string sharedAlleleName1 = "111:111";
            const string sharedAlleleName2 = "999:999";
            const string sharedPGroup = "shared-p-group";
            var gDnaStatus = new AlleleTypingStatus(SequenceStatus.Full, DnaCategory.GDna);
            var cDnaStatus = new AlleleTypingStatus(SequenceStatus.Full, DnaCategory.CDna);

            var patientResult1 = new HlaScoringMetadataBuilder()
                .AtLocus(MatchedLocus)
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithAlleleName(sharedAlleleName1)
                    .WithAlleleTypingStatus(gDnaStatus)
                    .WithMatchingPGroup(sharedPGroup)
                    .Build())
                .Build();

            var patientResult2 = new HlaScoringMetadataBuilder()
                .AtLocus(MatchedLocus)
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithAlleleName(sharedAlleleName2)
                    .WithAlleleTypingStatus(cDnaStatus)
                    .WithMatchingPGroup(sharedPGroup)
                    .Build())
                .Build();

            var donorResult1 = new HlaScoringMetadataBuilder()
                .AtLocus(MatchedLocus)
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithAlleleName(sharedAlleleName2)
                    .WithAlleleTypingStatus(cDnaStatus)
                    .WithMatchingPGroup(sharedPGroup)
                    .Build())
                .Build();

            var donorResult2 = new HlaScoringMetadataBuilder()
                .AtLocus(MatchedLocus)
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithAlleleName(sharedAlleleName1)
                    .WithAlleleTypingStatus(gDnaStatus)
                    .WithMatchingPGroup(sharedPGroup)
                    .Build())
                .Build();

            var patientLookupResults = BuildMetadataAtMatchedLocus(patientResult1, patientResult2);
            var donorLookupResults = BuildMetadataAtMatchedLocus(donorResult1, donorResult2);
            var actualGradingResults = gradingService.CalculateGrades(patientLookupResults, donorLookupResults);

            var expectedMatchOrientations = new[] {MatchOrientation.Cross};
            var expectedGradingResult1 = new MatchGradeResult(MatchGrade.CDna, expectedMatchOrientations);
            var expectedGradingResult2 = new MatchGradeResult(MatchGrade.GDna, expectedMatchOrientations);

            // Direct grade (P1: D1) is PGroup; Cross (P1: D2) is CDna
            actualGradingResults.A.Position1.Should().BeEquivalentTo(expectedGradingResult1);
            // Direct grade (P2: D2) is PGroup; Cross (P2: D1) is GDna
            actualGradingResults.A.Position2.Should().BeEquivalentTo(expectedGradingResult2);
        }

        [Test]
        public void CalculateGrades_WorseMatchWithMismatchInDirect_BetterMatchWithMismatchInCross_ReturnsBetterMatchWithMismatchInCrossOrientation()
        {
            const string matchingAssociatedName = "associated";
            const string matchingSplitName = "matching-split";
            var betterMatchingSerologies = new[]
            {
                new SerologyEntry(matchingAssociatedName, SerologySubtype.Associated, true),
                new SerologyEntry(matchingSplitName, SerologySubtype.Split, false)
            };

            var patientResult1 = new HlaScoringMetadataBuilder()
                .AtLocus(MatchedLocus)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithMatchingSerologies(betterMatchingSerologies)
                    .Build())
                .Build();

            var patientResult2 = new HlaScoringMetadataBuilder()
                .AtLocus(MatchedLocus)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithMatchingSerologies(new[] {new SerologyEntry("mismatched-not-split", SerologySubtype.NotSplit, true)})
                    .Build())
                .Build();

            var donorResult1 = new HlaScoringMetadataBuilder()
                .AtLocus(MatchedLocus)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithMatchingSerologies(new[]
                    {
                        new SerologyEntry(matchingSplitName, SerologySubtype.Split, true),
                        new SerologyEntry(matchingAssociatedName, SerologySubtype.Associated, false)
                    })
                    .Build())
                .Build();

            var donorResult2 = new HlaScoringMetadataBuilder()
                .AtLocus(MatchedLocus)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithMatchingSerologies(betterMatchingSerologies)
                    .Build())
                .Build();

            var patientLookupResults = BuildMetadataAtMatchedLocus(patientResult1, patientResult2);
            var donorLookupResults = BuildMetadataAtMatchedLocus(donorResult1, donorResult2);
            var actualGradingResults = gradingService.CalculateGrades(patientLookupResults, donorLookupResults);

            var expectedMatchOrientations = new[] {MatchOrientation.Cross};
            var expectedGradingResult1 = new MatchGradeResult(MatchGrade.Mismatch, expectedMatchOrientations);
            var expectedGradingResult2 = new MatchGradeResult(MatchGrade.Associated, expectedMatchOrientations);

            // Direct grade (P1: D1) is Split; Cross (P1: D2) is Mismatch
            actualGradingResults.A.Position1.Should().BeEquivalentTo(expectedGradingResult1);
            // Direct grade (P2: D2) is Mismatch; Cross (P2: D1) is Associated
            actualGradingResults.A.Position2.Should().BeEquivalentTo(expectedGradingResult2);
        }

        [Test]
        public void CalculateGrades_TwoSameMatchesInDirect_TwoSameMatchesInCross_ReturnsTwoSameMatchesInBothOrientations()
        {
            const string sharedFirstTwoFields = "999:999";
            var fullSequenceStatus = new AlleleTypingStatus(SequenceStatus.Full, DnaCategory.CDna);

            var patientResult1 = new HlaScoringMetadataBuilder()
                .AtLocus(MatchedLocus)
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithAlleleName(sharedFirstTwoFields + ":01")
                    .WithAlleleTypingStatus(fullSequenceStatus)
                    .Build())
                .Build();

            var patientResult2 = new HlaScoringMetadataBuilder()
                .AtLocus(MatchedLocus)
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithAlleleName(sharedFirstTwoFields + ":02")
                    .WithAlleleTypingStatus(fullSequenceStatus)
                    .Build())
                .Build();

            var donorResult1 = new HlaScoringMetadataBuilder()
                .AtLocus(MatchedLocus)
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithAlleleName(sharedFirstTwoFields + ":03")
                    .WithAlleleTypingStatus(fullSequenceStatus)
                    .Build())
                .Build();

            var donorResult2 = new HlaScoringMetadataBuilder()
                .AtLocus(MatchedLocus)
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithAlleleName(sharedFirstTwoFields + ":04")
                    .WithAlleleTypingStatus(fullSequenceStatus)
                    .Build())
                .Build();

            var patientLookupResults = BuildMetadataAtMatchedLocus(patientResult1, patientResult2);
            var donorLookupResults = BuildMetadataAtMatchedLocus(donorResult1, donorResult2);
            var actualGradingResults = gradingService.CalculateGrades(patientLookupResults, donorLookupResults);

            var expectedMatchOrientations = new[] {MatchOrientation.Direct, MatchOrientation.Cross};
            var expectedGradingResult1 = new MatchGradeResult(MatchGrade.Protein, expectedMatchOrientations);
            var expectedGradingResult2 = new MatchGradeResult(MatchGrade.Protein, expectedMatchOrientations);

            // Direct grade (P1: D1) is Protein; Cross (P1: D2) is Protein
            actualGradingResults.A.Position1.Should().BeEquivalentTo(expectedGradingResult1);
            // Direct grade (P2: D2) is Protein; Cross (P2: D1) is Protein
            actualGradingResults.A.Position2.Should().BeEquivalentTo(expectedGradingResult2);
        }

        [Test]
        public void CalculateGrades_SameMatchAndMismatchInDirect_SameMatchAndMismatchInCross_ReturnsSameMatchAndMismatchInBothOrientations()
        {
            var directMatchingSerology = new SerologyEntry("matching-split", SerologySubtype.Split, true);

            var patientResult1 = new HlaScoringMetadataBuilder()
                .AtLocus(MatchedLocus)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithMatchingSerologies(new[] {directMatchingSerology})
                    .Build())
                .Build();

            var patientResult2 = new HlaScoringMetadataBuilder()
                .AtLocus(MatchedLocus)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithMatchingSerologies(new[] {directMatchingSerology})
                    .Build())
                .Build();

            var donorResult1 = new HlaScoringMetadataBuilder()
                .AtLocus(MatchedLocus)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithMatchingSerologies(new[] {new SerologyEntry("mismatched-serology", SerologySubtype.NotSplit, true)})
                    .Build())
                .Build();

            var donorResult2 = new HlaScoringMetadataBuilder()
                .AtLocus(MatchedLocus)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithMatchingSerologies(new[] {directMatchingSerology})
                    .Build())
                .Build();

            var patientLookupResults = BuildMetadataAtMatchedLocus(patientResult1, patientResult2);
            var donorLookupResults = BuildMetadataAtMatchedLocus(donorResult1, donorResult2);
            var actualGradingResults = gradingService.CalculateGrades(patientLookupResults, donorLookupResults);

            var expectedMatchOrientations = new[] {MatchOrientation.Direct, MatchOrientation.Cross};
            var expectedGradingResult1 = new MatchGradeResult(MatchGrade.Mismatch, expectedMatchOrientations);
            var expectedGradingResult2 = new MatchGradeResult(MatchGrade.Split, expectedMatchOrientations);

            // Direct grade (P1: D1) is Mismatch; Cross (P1: D2) is Split
            actualGradingResults.A.Position1.Should().BeEquivalentTo(expectedGradingResult1);
            // Direct grade (P2: D2) is Split; Cross (P2: D1) is Mismatch
            actualGradingResults.A.Position2.Should().BeEquivalentTo(expectedGradingResult2);
        }

        [Test]
        public void CalculateGrades_TwoMismatchesInDirect_TwoMismatchesInCross_ReturnTwoMismatchesInBothOrientations()
        {
            var patientResult1 = new HlaScoringMetadataBuilder()
                .AtLocus(MatchedLocus)
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithMatchingGGroup("patient-g-group-1")
                    .WithMatchingPGroup("patient-p-group-1")
                    .Build())
                .Build();

            var patientResult2 = new HlaScoringMetadataBuilder()
                .AtLocus(MatchedLocus)
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithMatchingGGroup("patient-g-group-2")
                    .WithMatchingPGroup("patient-p-group-2")
                    .Build())
                .Build();

            var donorResult1 = new HlaScoringMetadataBuilder()
                .AtLocus(MatchedLocus)
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithMatchingGGroup("donor-g-group-1")
                    .WithMatchingPGroup("donor-p-group-1")
                    .Build())
                .Build();

            var donorResult2 = new HlaScoringMetadataBuilder()
                .AtLocus(MatchedLocus)
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithMatchingGGroup("donor-g-group-2")
                    .WithMatchingPGroup("donor-p-group-2")
                    .Build())
                .Build();

            var patientLookupResults = BuildMetadataAtMatchedLocus(patientResult1, patientResult2);
            var donorLookupResults = BuildMetadataAtMatchedLocus(donorResult1, donorResult2);
            var actualGradingResults = gradingService.CalculateGrades(patientLookupResults, donorLookupResults);

            var expectedMatchOrientations = new[] {MatchOrientation.Direct, MatchOrientation.Cross};
            var expectedGradingResult1 = new MatchGradeResult(MatchGrade.Mismatch, expectedMatchOrientations);
            var expectedGradingResult2 = new MatchGradeResult(MatchGrade.Mismatch, expectedMatchOrientations);

            // Direct grade (P1: D1) is Mismatch; Cross (P1: D2) is Mismatch
            actualGradingResults.A.Position1.Should().BeEquivalentTo(expectedGradingResult1);
            // Direct grade (P2: D2) is Mismatch; Cross (P2: D1) is Mismatch
            actualGradingResults.A.Position2.Should().BeEquivalentTo(expectedGradingResult2);
        }

        [TestCase(typeof(SingleAlleleScoringInfo))]
        [TestCase(typeof(MultipleAlleleScoringInfo))]
        [TestCase(typeof(ConsolidatedMolecularScoringInfo))]
        [TestCase(typeof(SerologyScoringInfo))]
        public void CalculateGrades_PatientIsMissingTheLocusTyping_ReturnsTwoPGroupMatchesInBothOrientations(
            Type donorScoringInfoType)
        {
            var patientLookupResults = BuildMetadataAtMatchedLocus(null, null);

            var donorResult = new HlaScoringMetadataBuilder()
                .AtLocus(MatchedLocus)
                .WithHlaScoringInfo(ScoringInfoBuilderFactory.GetDefaultScoringInfoFromBuilder(donorScoringInfoType))
                .Build();
            var donorLookupResults = BuildMetadataAtMatchedLocus(donorResult, donorResult);

            var actualGradingResults = gradingService.CalculateGrades(patientLookupResults, donorLookupResults);

            var expectedMatchOrientations = new[] {MatchOrientation.Direct, MatchOrientation.Cross};
            var expectedGradingResult1 = new MatchGradeResult(MatchGrade.PGroup, expectedMatchOrientations);
            var expectedGradingResult2 = new MatchGradeResult(MatchGrade.PGroup, expectedMatchOrientations);

            actualGradingResults.A.Position1.Should().BeEquivalentTo(expectedGradingResult1);
            actualGradingResults.A.Position2.Should().BeEquivalentTo(expectedGradingResult2);
        }

        [TestCase(typeof(SingleAlleleScoringInfo))]
        [TestCase(typeof(MultipleAlleleScoringInfo))]
        [TestCase(typeof(ConsolidatedMolecularScoringInfo))]
        [TestCase(typeof(SerologyScoringInfo))]
        public void CalculateGrades_DonorIsMissingTheLocusTyping_ReturnsTwoPGroupMatchesInBothOrientations(
            Type patientScoringInfoType)
        {
            var patientResult = new HlaScoringMetadataBuilder()
                .AtLocus(MatchedLocus)
                .WithHlaScoringInfo(ScoringInfoBuilderFactory.GetDefaultScoringInfoFromBuilder(patientScoringInfoType))
                .Build();
            var patientLookupResults = BuildMetadataAtMatchedLocus(patientResult, patientResult);

            var donorLookupResults = BuildMetadataAtMatchedLocus(null, null);

            var actualGradingResults = gradingService.CalculateGrades(patientLookupResults, donorLookupResults);

            var expectedMatchOrientations = new[] {MatchOrientation.Direct, MatchOrientation.Cross};
            var expectedGradingResult1 = new MatchGradeResult(MatchGrade.PGroup, expectedMatchOrientations);
            var expectedGradingResult2 = new MatchGradeResult(MatchGrade.PGroup, expectedMatchOrientations);

            actualGradingResults.A.Position1.Should().BeEquivalentTo(expectedGradingResult1);
            actualGradingResults.A.Position2.Should().BeEquivalentTo(expectedGradingResult2);
        }

        [Test]
        public void CalculateGrades_PatientAndDonorAreBothMissingTheLocusTyping_ReturnsTwoPGroupMatchesInBothOrientations()
        {
            var patientLookupResults = BuildMetadataAtMatchedLocus(null, null);

            var donorLookupResults = BuildMetadataAtMatchedLocus(null, null);

            var actualGradingResults = gradingService.CalculateGrades(patientLookupResults, donorLookupResults);

            var expectedMatchOrientations = new[] {MatchOrientation.Direct, MatchOrientation.Cross};
            var expectedGradingResult1 = new MatchGradeResult(MatchGrade.PGroup, expectedMatchOrientations);
            var expectedGradingResult2 = new MatchGradeResult(MatchGrade.PGroup, expectedMatchOrientations);

            actualGradingResults.A.Position1.Should().BeEquivalentTo(expectedGradingResult1);
            actualGradingResults.A.Position2.Should().BeEquivalentTo(expectedGradingResult2);
        }

        #endregion

        #region Tests: Typing affects the maximum possible grade assigned

        [Test]
        public void CalculateGrades_PatientAndDonorHaveSameSingleAllele_ReturnsMaxGradeOfGDna()
        {
            var sharedSingleAlleleScoringInfo = new SingleAlleleScoringInfoBuilder()
                .WithAlleleName("999:999")
                .WithAlleleTypingStatus(new AlleleTypingStatus(SequenceStatus.Full, DnaCategory.GDna))
                .WithMatchingGGroup("shared-g-group")
                .WithMatchingPGroup("shared-p-group")
                .WithMatchingSerologies(new[]
                {
                    new SerologyEntry("shared-associated", SerologySubtype.Associated, true),
                    new SerologyEntry("shared-split", SerologySubtype.Split, true),
                    new SerologyEntry("shared-broad", SerologySubtype.Broad, true)
                })
                .Build();

            var patientResult = new HlaScoringMetadataBuilder()
                .AtLocus(MatchedLocus)
                .WithHlaScoringInfo(sharedSingleAlleleScoringInfo)
                .Build();

            var donorResult = new HlaScoringMetadataBuilder()
                .AtLocus(MatchedLocus)
                .WithHlaScoringInfo(sharedSingleAlleleScoringInfo)
                .Build();

            var patientLookupResults = BuildMetadataAtMatchedLocus(patientResult, defaultSerologyResult);
            var donorLookupResults = BuildMetadataAtMatchedLocus(donorResult, defaultSerologyResult);
            var actualGradingResults = gradingService.CalculateGrades(patientLookupResults, donorLookupResults);

            var expectedGradingResult = new MatchGradeResult(MatchGrade.GDna, new[] {MatchOrientation.Direct});

            actualGradingResults.A.Position1.Should().BeEquivalentTo(expectedGradingResult);
        }

        [Test]
        public void CalculateGrades_PatientHasSingleAllele_DonorHasMatchingMultipleAllele_ReturnsMaxGradeOfGDna()
        {
            var sharedSingleAlleleScoringInfo = new SingleAlleleScoringInfoBuilder()
                .WithAlleleName("999:999")
                .WithAlleleTypingStatus(new AlleleTypingStatus(SequenceStatus.Full, DnaCategory.GDna))
                .WithMatchingGGroup("shared-g-group")
                .WithMatchingPGroup("shared-p-group")
                .WithMatchingSerologies(new[]
                {
                    new SerologyEntry("shared-associated", SerologySubtype.Associated, true),
                    new SerologyEntry("shared-split", SerologySubtype.Split, true),
                    new SerologyEntry("shared-broad", SerologySubtype.Broad, true)
                })
                .Build();

            var patientResult = new HlaScoringMetadataBuilder()
                .AtLocus(MatchedLocus)
                .WithHlaScoringInfo(sharedSingleAlleleScoringInfo)
                .Build();

            var donorResult = new HlaScoringMetadataBuilder()
                .AtLocus(MatchedLocus)
                .WithHlaScoringInfo(new MultipleAlleleScoringInfoBuilder()
                    .WithAlleleScoringInfos(new[] {sharedSingleAlleleScoringInfo})
                    .Build())
                .Build();

            var patientLookupResults = BuildMetadataAtMatchedLocus(patientResult, defaultSerologyResult);
            var donorLookupResults = BuildMetadataAtMatchedLocus(donorResult, defaultSerologyResult);
            var actualGradingResults = gradingService.CalculateGrades(patientLookupResults, donorLookupResults);

            var expectedGradingResult = new MatchGradeResult(MatchGrade.GDna, new[] {MatchOrientation.Direct});

            actualGradingResults.A.Position1.Should().BeEquivalentTo(expectedGradingResult);
        }

        [Test]
        public void CalculateGrades_PatientHasSingleAllele_DonorHasMatchingConsolidatedMolecular_ReturnsMaxGradeOfGGroup()
        {
            const string sharedGGroup = "shared-g-group";
            const string sharedPGroup = "shared-p-group";
            var sharedMatchingSerologies = new[]
            {
                new SerologyEntry("shared-associated", SerologySubtype.Associated, true),
                new SerologyEntry("shared-split", SerologySubtype.Split, true),
                new SerologyEntry("shared-broad", SerologySubtype.Broad, true)
            };

            var patientResult = new HlaScoringMetadataBuilder()
                .AtLocus(MatchedLocus)
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithAlleleName("999:999")
                    .WithAlleleTypingStatus(new AlleleTypingStatus(SequenceStatus.Full, DnaCategory.GDna))
                    .WithMatchingGGroup(sharedGGroup)
                    .WithMatchingPGroup(sharedPGroup)
                    .WithMatchingSerologies(sharedMatchingSerologies)
                    .Build())
                .Build();

            var donorResult = new HlaScoringMetadataBuilder()
                .AtLocus(MatchedLocus)
                .WithHlaScoringInfo(new ConsolidatedMolecularScoringInfoBuilder()
                    .WithMatchingGGroups(new[] {sharedGGroup})
                    .WithMatchingPGroups(new[] {sharedPGroup})
                    .WithMatchingSerologies(sharedMatchingSerologies)
                    .Build())
                .Build();

            var patientLookupResults = BuildMetadataAtMatchedLocus(patientResult, defaultSerologyResult);
            var donorLookupResults = BuildMetadataAtMatchedLocus(donorResult, defaultSerologyResult);
            var actualGradingResults = gradingService.CalculateGrades(patientLookupResults, donorLookupResults);

            var expectedGradingResult = new MatchGradeResult(MatchGrade.GGroup, new[] {MatchOrientation.Direct});

            actualGradingResults.A.Position1.Should().BeEquivalentTo(expectedGradingResult);
        }

        [Test]
        public void CalculateGrades_PatientHasSingleAllele_DonorHasMatchingSerology_ReturnsMaxGradeOfAssociated()
        {
            var sharedMatchingSerologies = new[]
            {
                new SerologyEntry("shared-associated", SerologySubtype.Associated, true),
                new SerologyEntry("shared-split", SerologySubtype.Split, true),
                new SerologyEntry("shared-broad", SerologySubtype.Broad, true)
            };

            var patientResult = new HlaScoringMetadataBuilder()
                .AtLocus(MatchedLocus)
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithAlleleName("999:999")
                    .WithAlleleTypingStatus(new AlleleTypingStatus(SequenceStatus.Full, DnaCategory.GDna))
                    .WithMatchingGGroup("patient-g-group")
                    .WithMatchingPGroup("patient-p-group")
                    .WithMatchingSerologies(sharedMatchingSerologies)
                    .Build())
                .Build();

            var donorResult = new HlaScoringMetadataBuilder()
                .AtLocus(MatchedLocus)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithMatchingSerologies(sharedMatchingSerologies)
                    .Build())
                .Build();

            var patientLookupResults = BuildMetadataAtMatchedLocus(patientResult, defaultSerologyResult);
            var donorLookupResults = BuildMetadataAtMatchedLocus(donorResult, defaultSerologyResult);
            var actualGradingResults = gradingService.CalculateGrades(patientLookupResults, donorLookupResults);

            var expectedGradingResult = new MatchGradeResult(MatchGrade.Associated, new[] {MatchOrientation.Direct});

            actualGradingResults.A.Position1.Should().BeEquivalentTo(expectedGradingResult);
        }

        [Test]
        public void CalculateGrades_PatientHasMultipleAllele_DonorHasMatchingSingleAllele_ReturnsMaxGradeOfGDna()
        {
            var sharedSingleAlleleScoringInfo = new SingleAlleleScoringInfoBuilder()
                .WithAlleleName("999:999")
                .WithAlleleTypingStatus(new AlleleTypingStatus(SequenceStatus.Full, DnaCategory.GDna))
                .WithMatchingGGroup("shared-g-group")
                .WithMatchingPGroup("shared-p-group")
                .WithMatchingSerologies(new[]
                {
                    new SerologyEntry("shared-associated", SerologySubtype.Associated, true),
                    new SerologyEntry("shared-split", SerologySubtype.Split, true),
                    new SerologyEntry("shared-broad", SerologySubtype.Broad, true)
                })
                .Build();

            var patientResult = new HlaScoringMetadataBuilder()
                .AtLocus(MatchedLocus)
                .WithHlaScoringInfo(new MultipleAlleleScoringInfoBuilder()
                    .WithAlleleScoringInfos(new[] {sharedSingleAlleleScoringInfo})
                    .Build())
                .Build();

            var donorResult = new HlaScoringMetadataBuilder()
                .AtLocus(MatchedLocus)
                .WithHlaScoringInfo(sharedSingleAlleleScoringInfo)
                .Build();

            var patientLookupResults = BuildMetadataAtMatchedLocus(patientResult, defaultSerologyResult);
            var donorLookupResults = BuildMetadataAtMatchedLocus(donorResult, defaultSerologyResult);
            var actualGradingResults = gradingService.CalculateGrades(patientLookupResults, donorLookupResults);

            var expectedGradingResult = new MatchGradeResult(MatchGrade.GDna, new[] {MatchOrientation.Direct});

            actualGradingResults.A.Position1.Should().BeEquivalentTo(expectedGradingResult);
        }

        [Test]
        public void CalculateGrades_PatientAndDonorHaveSameMultipleAllele_ReturnsMaxGradeOfGDna()
        {
            var sharedSingleAlleleScoringInfo = new SingleAlleleScoringInfoBuilder()
                .WithAlleleName("999:999")
                .WithAlleleTypingStatus(new AlleleTypingStatus(SequenceStatus.Full, DnaCategory.GDna))
                .WithMatchingGGroup("shared-g-group")
                .WithMatchingPGroup("shared-p-group")
                .WithMatchingSerologies(new[]
                {
                    new SerologyEntry("shared-associated", SerologySubtype.Associated, true),
                    new SerologyEntry("shared-split", SerologySubtype.Split, true),
                    new SerologyEntry("shared-broad", SerologySubtype.Broad, true)
                })
                .Build();

            var patientResult = new HlaScoringMetadataBuilder()
                .AtLocus(MatchedLocus)
                .WithHlaScoringInfo(new MultipleAlleleScoringInfoBuilder()
                    .WithAlleleScoringInfos(new[] {sharedSingleAlleleScoringInfo})
                    .Build())
                .Build();

            var donorResult = new HlaScoringMetadataBuilder()
                .AtLocus(MatchedLocus)
                .WithHlaScoringInfo(new MultipleAlleleScoringInfoBuilder()
                    .WithAlleleScoringInfos(new[] {sharedSingleAlleleScoringInfo})
                    .Build())
                .Build();

            var patientLookupResults = BuildMetadataAtMatchedLocus(patientResult, defaultSerologyResult);
            var donorLookupResults = BuildMetadataAtMatchedLocus(donorResult, defaultSerologyResult);
            var actualGradingResults = gradingService.CalculateGrades(patientLookupResults, donorLookupResults);

            var expectedGradingResult = new MatchGradeResult(MatchGrade.GDna, new[] {MatchOrientation.Direct});

            actualGradingResults.A.Position1.Should().BeEquivalentTo(expectedGradingResult);
        }

        [Test]
        public void CalculateGrades_PatientHasMultipleAllele_DonorHasMatchingConsolidatedMolecular_ReturnsMaxGradeOfGGroup()
        {
            const string sharedGGroup = "shared-g-group";
            const string sharedPGroup = "shared-p-group";
            var sharedMatchingSerologies = new[]
            {
                new SerologyEntry("shared-associated", SerologySubtype.Associated, true),
                new SerologyEntry("shared-split", SerologySubtype.Split, true),
                new SerologyEntry("shared-broad", SerologySubtype.Broad, true)
            };

            var patientResult = new HlaScoringMetadataBuilder()
                .AtLocus(MatchedLocus)
                .WithHlaScoringInfo(new MultipleAlleleScoringInfoBuilder()
                    .WithAlleleScoringInfos(new[]
                    {
                        new SingleAlleleScoringInfoBuilder()
                            .WithAlleleName("999:999")
                            .WithAlleleTypingStatus(new AlleleTypingStatus(SequenceStatus.Full, DnaCategory.GDna))
                            .WithMatchingGGroup(sharedGGroup)
                            .WithMatchingPGroup(sharedPGroup)
                            .Build()
                    })
                    .WithMatchingSerologies(sharedMatchingSerologies)
                    .Build())
                .Build();

            var donorResult = new HlaScoringMetadataBuilder()
                .AtLocus(MatchedLocus)
                .WithHlaScoringInfo(new ConsolidatedMolecularScoringInfoBuilder()
                    .WithMatchingGGroups(new[] {sharedGGroup})
                    .WithMatchingPGroups(new[] {sharedPGroup})
                    .WithMatchingSerologies(sharedMatchingSerologies)
                    .Build())
                .Build();

            var patientLookupResults = BuildMetadataAtMatchedLocus(patientResult, defaultSerologyResult);
            var donorLookupResults = BuildMetadataAtMatchedLocus(donorResult, defaultSerologyResult);
            var actualGradingResults = gradingService.CalculateGrades(patientLookupResults, donorLookupResults);

            var expectedGradingResult = new MatchGradeResult(MatchGrade.GGroup, new[] {MatchOrientation.Direct});

            actualGradingResults.A.Position1.Should().BeEquivalentTo(expectedGradingResult);
        }

        [Test]
        public void CalculateGrades_PatientHasMultipleAllele_DonorHasMatchingSerology_ReturnsMaxGradeOfAssociated()
        {
            var sharedMatchingSerologies = new[]
            {
                new SerologyEntry("shared-associated", SerologySubtype.Associated, true),
                new SerologyEntry("shared-split", SerologySubtype.Split, true),
                new SerologyEntry("shared-broad", SerologySubtype.Broad, true)
            };

            var patientResult = new HlaScoringMetadataBuilder()
                .AtLocus(MatchedLocus)
                .WithHlaScoringInfo(new MultipleAlleleScoringInfoBuilder()
                    .WithAlleleScoringInfos(new[]
                    {
                        new SingleAlleleScoringInfoBuilder()
                            .WithAlleleName("999:999")
                            .WithAlleleTypingStatus(new AlleleTypingStatus(SequenceStatus.Full, DnaCategory.GDna))
                            .WithMatchingGGroup("patient-g-group")
                            .WithMatchingPGroup("patient-p-group")
                            .Build()
                    })
                    .WithMatchingSerologies(sharedMatchingSerologies)
                    .Build())
                .Build();

            var donorResult = new HlaScoringMetadataBuilder()
                .AtLocus(MatchedLocus)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithMatchingSerologies(sharedMatchingSerologies)
                    .Build())
                .Build();

            var patientLookupResults = BuildMetadataAtMatchedLocus(patientResult, defaultSerologyResult);
            var donorLookupResults = BuildMetadataAtMatchedLocus(donorResult, defaultSerologyResult);
            var actualGradingResults = gradingService.CalculateGrades(patientLookupResults, donorLookupResults);

            var expectedGradingResult = new MatchGradeResult(MatchGrade.Associated, new[] {MatchOrientation.Direct});

            actualGradingResults.A.Position1.Should().BeEquivalentTo(expectedGradingResult);
        }

        [Test]
        public void CalculateGrades_PatientHasConsolidatedMolecular_DonorHasMatchingSingleAllele_ReturnsMaxGradeOfGGroup()
        {
            const string sharedGGroup = "shared-g-group";
            const string sharedPGroup = "shared-p-group";
            var sharedMatchingSerologies = new[]
            {
                new SerologyEntry("shared-associated", SerologySubtype.Associated, true),
                new SerologyEntry("shared-split", SerologySubtype.Split, true),
                new SerologyEntry("shared-broad", SerologySubtype.Broad, true)
            };

            var patientResult = new HlaScoringMetadataBuilder()
                .AtLocus(MatchedLocus)
                .WithHlaScoringInfo(new ConsolidatedMolecularScoringInfoBuilder()
                    .WithMatchingGGroups(new[] {sharedGGroup})
                    .WithMatchingPGroups(new[] {sharedPGroup})
                    .WithMatchingSerologies(sharedMatchingSerologies)
                    .Build())
                .Build();

            var donorResult = new HlaScoringMetadataBuilder()
                .AtLocus(MatchedLocus)
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithAlleleName("999:999")
                    .WithAlleleTypingStatus(new AlleleTypingStatus(SequenceStatus.Full, DnaCategory.GDna))
                    .WithMatchingGGroup(sharedGGroup)
                    .WithMatchingPGroup(sharedPGroup)
                    .WithMatchingSerologies(sharedMatchingSerologies)
                    .Build())
                .Build();

            var patientLookupResults = BuildMetadataAtMatchedLocus(patientResult, defaultSerologyResult);
            var donorLookupResults = BuildMetadataAtMatchedLocus(donorResult, defaultSerologyResult);
            var actualGradingResults = gradingService.CalculateGrades(patientLookupResults, donorLookupResults);

            var expectedGradingResult = new MatchGradeResult(MatchGrade.GGroup, new[] {MatchOrientation.Direct});

            actualGradingResults.A.Position1.Should().BeEquivalentTo(expectedGradingResult);
        }

        [Test]
        public void CalculateGrades_PatientHasConsolidatedMolecular_DonorHasMatchingMultipleAllele_ReturnsMaxGradeOfGGroup()
        {
            const string sharedGGroup = "shared-g-group";
            const string sharedPGroup = "shared-p-group";
            var sharedMatchingSerologies = new[]
            {
                new SerologyEntry("shared-associated", SerologySubtype.Associated, true),
                new SerologyEntry("shared-split", SerologySubtype.Split, true),
                new SerologyEntry("shared-broad", SerologySubtype.Broad, true)
            };

            var patientResult = new HlaScoringMetadataBuilder()
                .AtLocus(MatchedLocus)
                .WithHlaScoringInfo(new ConsolidatedMolecularScoringInfoBuilder()
                    .WithMatchingGGroups(new[] {sharedGGroup})
                    .WithMatchingPGroups(new[] {sharedPGroup})
                    .WithMatchingSerologies(sharedMatchingSerologies)
                    .Build())
                .Build();

            var donorResult = new HlaScoringMetadataBuilder()
                .AtLocus(MatchedLocus)
                .WithHlaScoringInfo(new MultipleAlleleScoringInfoBuilder()
                    .WithAlleleScoringInfos(new[]
                    {
                        new SingleAlleleScoringInfoBuilder()
                            .WithAlleleName("999:999")
                            .WithAlleleTypingStatus(new AlleleTypingStatus(SequenceStatus.Full, DnaCategory.GDna))
                            .WithMatchingGGroup(sharedGGroup)
                            .WithMatchingPGroup(sharedPGroup)
                            .Build()
                    })
                    .WithMatchingSerologies(sharedMatchingSerologies)
                    .Build())
                .Build();

            var patientLookupResults = BuildMetadataAtMatchedLocus(patientResult, defaultSerologyResult);
            var donorLookupResults = BuildMetadataAtMatchedLocus(donorResult, defaultSerologyResult);
            var actualGradingResults = gradingService.CalculateGrades(patientLookupResults, donorLookupResults);

            var expectedGradingResult = new MatchGradeResult(MatchGrade.GGroup, new[] {MatchOrientation.Direct});

            actualGradingResults.A.Position1.Should().BeEquivalentTo(expectedGradingResult);
        }

        [Test]
        public void CalculateGrades_PatientAndDonorHaveSameConsolidatedMolecular_ReturnsMaxGradeOfGGroup()
        {
            const string sharedGGroup = "shared-g-group";
            const string sharedPGroup = "shared-p-group";
            var sharedMatchingSerologies = new[]
            {
                new SerologyEntry("shared-associated", SerologySubtype.Associated, true),
                new SerologyEntry("shared-split", SerologySubtype.Split, true),
                new SerologyEntry("shared-broad", SerologySubtype.Broad, true)
            };

            var patientResult = new HlaScoringMetadataBuilder()
                .AtLocus(MatchedLocus)
                .WithHlaScoringInfo(new ConsolidatedMolecularScoringInfoBuilder()
                    .WithMatchingGGroups(new[] {sharedGGroup})
                    .WithMatchingPGroups(new[] {sharedPGroup})
                    .WithMatchingSerologies(sharedMatchingSerologies)
                    .Build())
                .Build();

            var donorResult = new HlaScoringMetadataBuilder()
                .AtLocus(MatchedLocus)
                .WithHlaScoringInfo(new ConsolidatedMolecularScoringInfoBuilder()
                    .WithMatchingGGroups(new[] {sharedGGroup})
                    .WithMatchingPGroups(new[] {sharedPGroup})
                    .WithMatchingSerologies(sharedMatchingSerologies)
                    .Build())
                .Build();

            var patientLookupResults = BuildMetadataAtMatchedLocus(patientResult, defaultSerologyResult);
            var donorLookupResults = BuildMetadataAtMatchedLocus(donorResult, defaultSerologyResult);
            var actualGradingResults = gradingService.CalculateGrades(patientLookupResults, donorLookupResults);

            var expectedGradingResult = new MatchGradeResult(MatchGrade.GGroup, new[] {MatchOrientation.Direct});

            actualGradingResults.A.Position1.Should().BeEquivalentTo(expectedGradingResult);
        }

        [Test]
        public void CalculateGrades_PatientHasConsolidatedMolecular_DonorHasMatchingSerology_ReturnsMaxGradeOfAssociated()
        {
            var sharedMatchingSerologies = new[]
            {
                new SerologyEntry("shared-associated", SerologySubtype.Associated, true),
                new SerologyEntry("shared-split", SerologySubtype.Split, true),
                new SerologyEntry("shared-broad", SerologySubtype.Broad, true)
            };

            var patientResult = new HlaScoringMetadataBuilder()
                .AtLocus(MatchedLocus)
                .WithHlaScoringInfo(new ConsolidatedMolecularScoringInfoBuilder()
                    .WithMatchingGGroups(new[] {"patient-g-group"})
                    .WithMatchingPGroups(new[] {"patient-p-group"})
                    .WithMatchingSerologies(sharedMatchingSerologies)
                    .Build())
                .Build();

            var donorResult = new HlaScoringMetadataBuilder()
                .AtLocus(MatchedLocus)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithMatchingSerologies(sharedMatchingSerologies)
                    .Build())
                .Build();

            var patientLookupResults = BuildMetadataAtMatchedLocus(patientResult, defaultSerologyResult);
            var donorLookupResults = BuildMetadataAtMatchedLocus(donorResult, defaultSerologyResult);
            var actualGradingResults = gradingService.CalculateGrades(patientLookupResults, donorLookupResults);

            var expectedGradingResult = new MatchGradeResult(MatchGrade.Associated, new[] {MatchOrientation.Direct});

            actualGradingResults.A.Position1.Should().BeEquivalentTo(expectedGradingResult);
        }

        [Test]
        public void CalculateGrades_PatientHasSerology_DonorHasMatchingSingleAllele_ReturnsMaxGradeOfAssociated()
        {
            var sharedMatchingSerologies = new[]
            {
                new SerologyEntry("shared-associated", SerologySubtype.Associated, true),
                new SerologyEntry("shared-split", SerologySubtype.Split, true),
                new SerologyEntry("shared-broad", SerologySubtype.Broad, true)
            };

            var patientResult = new HlaScoringMetadataBuilder()
                .AtLocus(MatchedLocus)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithMatchingSerologies(sharedMatchingSerologies)
                    .Build())
                .Build();

            var donorResult = new HlaScoringMetadataBuilder()
                .AtLocus(MatchedLocus)
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithAlleleName("999:999")
                    .WithAlleleTypingStatus(new AlleleTypingStatus(SequenceStatus.Full, DnaCategory.GDna))
                    .WithMatchingGGroup("patient-g-group")
                    .WithMatchingPGroup("patient-p-group")
                    .WithMatchingSerologies(sharedMatchingSerologies)
                    .Build())
                .Build();

            var patientLookupResults = BuildMetadataAtMatchedLocus(patientResult, defaultSerologyResult);
            var donorLookupResults = BuildMetadataAtMatchedLocus(donorResult, defaultSerologyResult);
            var actualGradingResults = gradingService.CalculateGrades(patientLookupResults, donorLookupResults);

            var expectedGradingResult = new MatchGradeResult(MatchGrade.Associated, new[] {MatchOrientation.Direct});

            actualGradingResults.A.Position1.Should().BeEquivalentTo(expectedGradingResult);
        }

        [Test]
        public void CalculateGrades_PatientHasSerology_DonorHasMatchingMultipleAllele_ReturnsMaxGradeOfAssociated()
        {
            var sharedMatchingSerologies = new[]
            {
                new SerologyEntry("shared-associated", SerologySubtype.Associated, true),
                new SerologyEntry("shared-split", SerologySubtype.Split, true),
                new SerologyEntry("shared-broad", SerologySubtype.Broad, true)
            };

            var patientResult = new HlaScoringMetadataBuilder()
                .AtLocus(MatchedLocus)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithMatchingSerologies(sharedMatchingSerologies)
                    .Build())
                .Build();

            var donorResult = new HlaScoringMetadataBuilder()
                .AtLocus(MatchedLocus)
                .WithHlaScoringInfo(new MultipleAlleleScoringInfoBuilder()
                    .WithAlleleScoringInfos(new[]
                    {
                        new SingleAlleleScoringInfoBuilder()
                            .WithAlleleName("999:999")
                            .WithAlleleTypingStatus(new AlleleTypingStatus(SequenceStatus.Full, DnaCategory.GDna))
                            .WithMatchingGGroup("patient-g-group")
                            .WithMatchingPGroup("patient-p-group")
                            .Build()
                    })
                    .WithMatchingSerologies(sharedMatchingSerologies)
                    .Build())
                .Build();

            var patientLookupResults = BuildMetadataAtMatchedLocus(patientResult, defaultSerologyResult);
            var donorLookupResults = BuildMetadataAtMatchedLocus(donorResult, defaultSerologyResult);
            var actualGradingResults = gradingService.CalculateGrades(patientLookupResults, donorLookupResults);

            var expectedGradingResult = new MatchGradeResult(MatchGrade.Associated, new[] {MatchOrientation.Direct});

            actualGradingResults.A.Position1.Should().BeEquivalentTo(expectedGradingResult);
        }

        [Test]
        public void CalculateGrades_PatientHasSerology_DonorHasMatchingConsolidatedMolecular_ReturnsMaxGradeOfAssociated()
        {
            var sharedMatchingSerologies = new[]
            {
                new SerologyEntry("shared-associated", SerologySubtype.Associated, true),
                new SerologyEntry("shared-split", SerologySubtype.Split, true),
                new SerologyEntry("shared-broad", SerologySubtype.Broad, true)
            };

            var patientResult = new HlaScoringMetadataBuilder()
                .AtLocus(MatchedLocus)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithMatchingSerologies(sharedMatchingSerologies)
                    .Build())
                .Build();

            var donorResult = new HlaScoringMetadataBuilder()
                .AtLocus(MatchedLocus)
                .WithHlaScoringInfo(new ConsolidatedMolecularScoringInfoBuilder()
                    .WithMatchingGGroups(new[] {"patient-g-group"})
                    .WithMatchingPGroups(new[] {"patient-p-group"})
                    .WithMatchingSerologies(sharedMatchingSerologies)
                    .Build())
                .Build();

            var patientLookupResults = BuildMetadataAtMatchedLocus(patientResult, defaultSerologyResult);
            var donorLookupResults = BuildMetadataAtMatchedLocus(donorResult, defaultSerologyResult);
            var actualGradingResults = gradingService.CalculateGrades(patientLookupResults, donorLookupResults);

            var expectedGradingResult = new MatchGradeResult(MatchGrade.Associated, new[] {MatchOrientation.Direct});

            actualGradingResults.A.Position1.Should().BeEquivalentTo(expectedGradingResult);
        }

        [Test]
        public void CalculateGrades_PatientAndDonorHaveSameSerology_ReturnsMaxGradeOfAssociated()
        {
            var sharedMatchingSerologies = new[]
            {
                new SerologyEntry("shared-associated", SerologySubtype.Associated, true),
                new SerologyEntry("shared-split", SerologySubtype.Split, true),
                new SerologyEntry("shared-broad", SerologySubtype.Broad, true)
            };

            var patientResult = new HlaScoringMetadataBuilder()
                .AtLocus(MatchedLocus)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithMatchingSerologies(sharedMatchingSerologies)
                    .Build())
                .Build();

            var donorResult = new HlaScoringMetadataBuilder()
                .AtLocus(MatchedLocus)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithMatchingSerologies(sharedMatchingSerologies)
                    .Build())
                .Build();

            var patientLookupResults = BuildMetadataAtMatchedLocus(patientResult, defaultSerologyResult);
            var donorLookupResults = BuildMetadataAtMatchedLocus(donorResult, defaultSerologyResult);
            var actualGradingResults = gradingService.CalculateGrades(patientLookupResults, donorLookupResults);

            var expectedGradingResult = new MatchGradeResult(MatchGrade.Associated, new[] {MatchOrientation.Direct});

            actualGradingResults.A.Position1.Should().BeEquivalentTo(expectedGradingResult);
        }

        #endregion

        [Test]
        public void CalculateGrades_CalculatesMatchesForMultipleLoci()
        {
            var singleAlleleAtA = new HlaScoringMetadataBuilder()
                .AtLocus(Locus.A)
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithAlleleName("999:999")
                    .WithAlleleTypingStatus(new AlleleTypingStatus(SequenceStatus.Full, DnaCategory.GDna))
                    .Build())
                .Build();

            var consolidatedMolecularAtB = new HlaScoringMetadataBuilder()
                .AtLocus(Locus.B)
                .WithHlaScoringInfo(new ConsolidatedMolecularScoringInfoBuilder()
                    .WithMatchingGGroups(new[] {"shared-g-group"})
                    .Build())
                .Build();

            var serologyAtDrb1 = new HlaScoringMetadataBuilder()
                .AtLocus(Locus.Drb1)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithMatchingSerologies(new[] {new SerologyEntry("shared-not-split", SerologySubtype.NotSplit, true)})
                    .Build())
                .Build();

            var patientLookupResults = new PhenotypeInfo<IHlaScoringMetadata>()
                .SetLocus(Locus.A, singleAlleleAtA)
                .SetLocus(Locus.B, consolidatedMolecularAtB)
                .SetLocus(Locus.Drb1, serologyAtDrb1);

            var donorLookupResults = new PhenotypeInfo<IHlaScoringMetadata>()
                .SetLocus(Locus.A, singleAlleleAtA)
                .SetLocus(Locus.B, consolidatedMolecularAtB)
                .SetLocus(Locus.Drb1, serologyAtDrb1);

            var actualGradingResults = gradingService.CalculateGrades(patientLookupResults, donorLookupResults);

            var expectedMatchOrientations = new[] {MatchOrientation.Direct, MatchOrientation.Cross};
            var expectedGradingResultAtA = new MatchGradeResult(MatchGrade.GDna, expectedMatchOrientations);
            var expectedGradingResultAtB = new MatchGradeResult(MatchGrade.GGroup, expectedMatchOrientations);
            var expectedGradingResultAtDrb1 = new MatchGradeResult(MatchGrade.Split, expectedMatchOrientations);

            // both grades should be GDna, in both orientations
            actualGradingResults.A.Position1.Should().BeEquivalentTo(expectedGradingResultAtA);
            actualGradingResults.A.Position2.Should().BeEquivalentTo(expectedGradingResultAtA);
            // both grades should be GGroup, in both orientations
            actualGradingResults.B.Position1.Should().BeEquivalentTo(expectedGradingResultAtB);
            actualGradingResults.B.Position2.Should().BeEquivalentTo(expectedGradingResultAtB);
            // both grades should be Split, in both orientations
            actualGradingResults.Drb1.Position1.Should().BeEquivalentTo(expectedGradingResultAtDrb1);
            actualGradingResults.Drb1.Position2.Should().BeEquivalentTo(expectedGradingResultAtDrb1);
        }

        private static PhenotypeInfo<IHlaScoringMetadata> BuildMetadataAtMatchedLocus(
            IHlaScoringMetadata positionOneResult,
            IHlaScoringMetadata positionTwoResult)
        {
            return new PhenotypeInfo<IHlaScoringMetadata>()
                .SetPosition(MatchedLocus, LocusPosition.One, positionOneResult)
                .SetPosition(MatchedLocus, LocusPosition.Two, positionTwoResult);
        }
    }
}