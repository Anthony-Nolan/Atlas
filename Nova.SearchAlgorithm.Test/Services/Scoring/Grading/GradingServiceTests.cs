using FluentAssertions;
using Nova.SearchAlgorithm.Client.Models.SearchResults;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Models.Scoring;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.ScoringLookup;
using Nova.SearchAlgorithm.Services.Scoring.Grading;
using Nova.SearchAlgorithm.Test.Builders;
using Nova.SearchAlgorithm.Test.Builders.ScoringInfo;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.Services.Scoring.Grading
{
    public class GradingServiceTests
    {
        private IGradingService gradingService;
        private const Locus MatchedLocus = Locus.A;

        [SetUp]
        public void SetUpBeforeEachTest()
        {
            gradingService = new GradingService();
        }

        #region Tests: Best Grades & Orientation(s) returned

        [Test]
        public void CalculateGrades_TwoMatchesInDirect_TwoMismatchesInCross_ReturnsTwoMatchGradesInDirectOrientation()
        {
            const string sharedPGroup1 = "p-group-1";
            const string sharedPGroup2 = "p-group-2";

            var patientResult1 = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithMatchingPGroup(sharedPGroup1)
                    .Build())
                .Build();

            var patientResult2 = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithMatchingPGroup(sharedPGroup2)
                    .Build())
                .Build();

            var donorResult1 = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithMatchingPGroup(sharedPGroup1)
                    .Build())
                .Build();

            var donorResult2 = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithMatchingPGroup(sharedPGroup2)
                    .Build())
                .Build();

            var patientLookupResults = BuildLookupResultsAtMatchedLocus(patientResult1, patientResult2);
            var donorLookupResults = BuildLookupResultsAtMatchedLocus(donorResult1, donorResult2);
            var actualGradingResults = gradingService.CalculateGrades(patientLookupResults, donorLookupResults);

            var expectedMatchOrientations = new[] { MatchOrientation.Direct };
            var expectedGradingResult1 = new MatchGradeResult(MatchGrade.PGroup, expectedMatchOrientations);
            var expectedGradingResult2 = new MatchGradeResult(MatchGrade.PGroup, expectedMatchOrientations);

            // Direct grade (P1: D1) is PGroup; Cross (P1: D2) is Mismatch
            actualGradingResults.A_1.ShouldBeEquivalentTo(expectedGradingResult1);
            // Direct grade (P2: D2) is PGroup; Cross (P2: D1) is Mismatch
            actualGradingResults.A_2.ShouldBeEquivalentTo(expectedGradingResult2);
        }

        [Test]
        public void CalculateGrades_TwoBetterMatchesInDirect_TwoWorseMatchesInCross_ReturnsTwoBetterMatchGradesInDirectOrientation()
        {
            const string sharedAlleleName1 = "111:111";
            const string sharedAlleleName2 = "999:999";
            const string sharedPGroup = "shared-p-group";
            var gDnaStatus = new AlleleTypingStatus(SequenceStatus.Full, DnaCategory.GDna);
            var cDnaStatus = new AlleleTypingStatus(SequenceStatus.Full, DnaCategory.CDna);

            var patientResult1 = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithAlleleName(sharedAlleleName1)
                    .WithAlleleTypingStatus(gDnaStatus)
                    .WithMatchingPGroup(sharedPGroup)
                    .Build())
                .Build();

            var patientResult2 = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithAlleleName(sharedAlleleName2)
                    .WithAlleleTypingStatus(cDnaStatus)
                    .WithMatchingPGroup(sharedPGroup)
                    .Build())
                .Build();

            var patientLookupResults = new PhenotypeInfo<IHlaScoringLookupResult>();
            patientLookupResults.SetAtLocus(MatchedLocus, TypePositions.One, patientResult1);
            patientLookupResults.SetAtLocus(MatchedLocus, TypePositions.Two, patientResult2);

            var donorResult1 = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithAlleleName(sharedAlleleName1)
                    .WithAlleleTypingStatus(gDnaStatus)
                    .WithMatchingPGroup(sharedPGroup)
                    .Build())
                .Build();

            var donorResult2 = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithAlleleName(sharedAlleleName2)
                    .WithAlleleTypingStatus(cDnaStatus)
                    .WithMatchingPGroup(sharedPGroup)
                    .Build())
                .Build();

            var donorLookupResults = new PhenotypeInfo<IHlaScoringLookupResult>();
            donorLookupResults.SetAtLocus(MatchedLocus, TypePositions.One, donorResult1);
            donorLookupResults.SetAtLocus(MatchedLocus, TypePositions.Two, donorResult2);

            var actualGradingResults = gradingService.CalculateGrades(patientLookupResults, donorLookupResults);

            var expectedMatchOrientations = new[] { MatchOrientation.Direct };
            var expectedGradingResult1 = new MatchGradeResult(MatchGrade.GDna, expectedMatchOrientations);
            var expectedGradingResult2 = new MatchGradeResult(MatchGrade.CDna, expectedMatchOrientations);

            // Direct grade (P1: D1) is GDna; Cross (P1: D2) is PGroup
            actualGradingResults.A_1.ShouldBeEquivalentTo(expectedGradingResult1);
            // Direct grade (P2: D2) is CDna; Cross (P2: D1) is PGroup
            actualGradingResults.A_2.ShouldBeEquivalentTo(expectedGradingResult2);
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

            var patientResult1 = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithMatchingSerologies(betterMatchingSerologies)
                    .Build())
                .Build();

            var patientResult2 = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithMatchingSerologies(new[] { new SerologyEntry("mismatched-not-split", SerologySubtype.NotSplit, true) })
                    .Build())
                .Build();

            var donorResult1 = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithMatchingSerologies(betterMatchingSerologies)
                    .Build())
                .Build();

            var donorResult2 = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithMatchingSerologies(new[]
                    {
                        new SerologyEntry(matchingSplitName, SerologySubtype.Split, true),
                        new SerologyEntry(matchingAssociatedName, SerologySubtype.Associated, false)
                    })
                    .Build())
                .Build();

            var patientLookupResults = BuildLookupResultsAtMatchedLocus(patientResult1, patientResult2);
            var donorLookupResults = BuildLookupResultsAtMatchedLocus(donorResult1, donorResult2);
            var actualGradingResults = gradingService.CalculateGrades(patientLookupResults, donorLookupResults);

            var expectedMatchOrientations = new[] { MatchOrientation.Direct };
            var expectedGradingResult1 = new MatchGradeResult(MatchGrade.Associated, expectedMatchOrientations);
            var expectedGradingResult2 = new MatchGradeResult(MatchGrade.Mismatch, expectedMatchOrientations);

            // Direct grade (P1: D1) is Associated; Cross (P1: D2) is Split
            actualGradingResults.A_1.ShouldBeEquivalentTo(expectedGradingResult1);
            // Direct grade (P2: D2) is Mismatch; Cross (P2: D1) is Mismatch
            actualGradingResults.A_2.ShouldBeEquivalentTo(expectedGradingResult2);
        }

        [Test]
        public void CalculateGrades_TwoMismatchesInDirect_TwoMatchesInCross_ReturnsTwoMatchesInCrossOrientation()
        {
            const string sharedPGroup1 = "p-group-1";
            const string sharedPGroup2 = "p-group-2";

            var patientResult1 = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithMatchingPGroup(sharedPGroup1)
                    .Build())
                .Build();

            var patientResult2 = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithMatchingPGroup(sharedPGroup2)
                    .Build())
                .Build();

            var donorResult1 = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithMatchingPGroup(sharedPGroup2)
                    .Build())
                .Build();

            var donorResult2 = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithMatchingPGroup(sharedPGroup1)
                    .Build())
                .Build();

            var patientLookupResults = BuildLookupResultsAtMatchedLocus(patientResult1, patientResult2);
            var donorLookupResults = BuildLookupResultsAtMatchedLocus(donorResult1, donorResult2);
            var actualGradingResults = gradingService.CalculateGrades(patientLookupResults, donorLookupResults);

            var expectedMatchOrientations = new[] { MatchOrientation.Cross };
            var expectedGradingResult1 = new MatchGradeResult(MatchGrade.PGroup, expectedMatchOrientations);
            var expectedGradingResult2 = new MatchGradeResult(MatchGrade.PGroup, expectedMatchOrientations);

            // Direct grade (P1: D1) is Mismatch; Cross (P1: D2) is PGroup
            actualGradingResults.A_1.ShouldBeEquivalentTo(expectedGradingResult1);
            // Direct grade (P2: D2) is Mismatch; Cross (P2: D1) is PGroup
            actualGradingResults.A_2.ShouldBeEquivalentTo(expectedGradingResult2);
        }

        [Test]
        public void CalculateGrades_TwoWorseMatchesInDirect_TwoBetterMatchesInCross_ReturnsTwoBetterMatchesInCrossOrientation()
        {
            const string sharedAlleleName1 = "111:111";
            const string sharedAlleleName2 = "999:999";
            const string sharedPGroup = "shared-p-group";
            var gDnaStatus = new AlleleTypingStatus(SequenceStatus.Full, DnaCategory.GDna);
            var cDnaStatus = new AlleleTypingStatus(SequenceStatus.Full, DnaCategory.CDna);

            var patientResult1 = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithAlleleName(sharedAlleleName1)
                    .WithAlleleTypingStatus(gDnaStatus)
                    .WithMatchingPGroup(sharedPGroup)
                    .Build())
                .Build();

            var patientResult2 = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithAlleleName(sharedAlleleName2)
                    .WithAlleleTypingStatus(cDnaStatus)
                    .WithMatchingPGroup(sharedPGroup)
                    .Build())
                .Build();

            var donorResult1 = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithAlleleName(sharedAlleleName2)
                    .WithAlleleTypingStatus(cDnaStatus)
                    .WithMatchingPGroup(sharedPGroup)
                    .Build())
                .Build();

            var donorResult2 = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithAlleleName(sharedAlleleName1)
                    .WithAlleleTypingStatus(gDnaStatus)
                    .WithMatchingPGroup(sharedPGroup)
                    .Build())
                .Build();

            var patientLookupResults = BuildLookupResultsAtMatchedLocus(patientResult1, patientResult2);
            var donorLookupResults = BuildLookupResultsAtMatchedLocus(donorResult1, donorResult2);
            var actualGradingResults = gradingService.CalculateGrades(patientLookupResults, donorLookupResults);

            var expectedMatchOrientations = new[] { MatchOrientation.Cross };
            var expectedGradingResult1 = new MatchGradeResult(MatchGrade.GDna, expectedMatchOrientations);
            var expectedGradingResult2 = new MatchGradeResult(MatchGrade.CDna, expectedMatchOrientations);

            // Direct grade (P1: D1) is PGroup; Cross (P1: D2) is GDna
            actualGradingResults.A_1.ShouldBeEquivalentTo(expectedGradingResult1);
            // Direct grade (P2: D2) is PGroup; Cross (P2: D1) is CDna
            actualGradingResults.A_2.ShouldBeEquivalentTo(expectedGradingResult2);
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

            var patientResult1 = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithMatchingSerologies(betterMatchingSerologies)
                    .Build())
                .Build();

            var patientResult2 = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithMatchingSerologies(new[] { new SerologyEntry("mismatched-not-split", SerologySubtype.NotSplit, true) })
                    .Build())
                .Build();

            var donorResult1 = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithMatchingSerologies(new[]
                    {
                        new SerologyEntry(matchingSplitName, SerologySubtype.Split, true),
                        new SerologyEntry(matchingAssociatedName, SerologySubtype.Associated, false)
                    })
                    .Build())
                .Build();

            var donorResult2 = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithMatchingSerologies(betterMatchingSerologies)
                    .Build())
                .Build();

            var patientLookupResults = BuildLookupResultsAtMatchedLocus(patientResult1, patientResult2);
            var donorLookupResults = BuildLookupResultsAtMatchedLocus(donorResult1, donorResult2);
            var actualGradingResults = gradingService.CalculateGrades(patientLookupResults, donorLookupResults);

            var expectedMatchOrientations = new[] { MatchOrientation.Cross };
            var expectedGradingResult1 = new MatchGradeResult(MatchGrade.Associated, expectedMatchOrientations);
            var expectedGradingResult2 = new MatchGradeResult(MatchGrade.Mismatch, expectedMatchOrientations);

            // Direct grade (P1: D1) is Split; Cross (P1: D2) is Associated
            actualGradingResults.A_1.ShouldBeEquivalentTo(expectedGradingResult1);
            // Direct grade (P2: D2) is Mismatch; Cross (P2: D1) is Mismatch
            actualGradingResults.A_2.ShouldBeEquivalentTo(expectedGradingResult2);
        }

        [Test]
        public void CalculateGrades_TwoSameMatchesInDirect_TwoSameMatchesInCross_ReturnsTwoSameMatchesInBothOrientations()
        {
            const string sharedFirstTwoFields = "999:999";
            var fullSequenceStatus = new AlleleTypingStatus(SequenceStatus.Full, DnaCategory.CDna);

            var patientResult1 = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithAlleleName(sharedFirstTwoFields + ":01")
                    .WithAlleleTypingStatus(fullSequenceStatus)
                    .Build())
                .Build();

            var patientResult2 = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithAlleleName(sharedFirstTwoFields + ":02")
                    .WithAlleleTypingStatus(fullSequenceStatus)
                    .Build())
                .Build();

            var donorResult1 = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithAlleleName(sharedFirstTwoFields + ":03")
                    .WithAlleleTypingStatus(fullSequenceStatus)
                    .Build())
                .Build();

            var donorResult2 = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithAlleleName(sharedFirstTwoFields + ":04")
                    .WithAlleleTypingStatus(fullSequenceStatus)
                    .Build())
                .Build();

            var patientLookupResults = BuildLookupResultsAtMatchedLocus(patientResult1, patientResult2);
            var donorLookupResults = BuildLookupResultsAtMatchedLocus(donorResult1, donorResult2);
            var actualGradingResults = gradingService.CalculateGrades(patientLookupResults, donorLookupResults);

            var expectedMatchOrientations = new[] { MatchOrientation.Direct, MatchOrientation.Cross };
            var expectedGradingResult1 = new MatchGradeResult(MatchGrade.Protein, expectedMatchOrientations);
            var expectedGradingResult2 = new MatchGradeResult(MatchGrade.Protein, expectedMatchOrientations);

            // Direct grade (P1: D1) is Protein; Cross (P1: D2) is Protein
            actualGradingResults.A_1.ShouldBeEquivalentTo(expectedGradingResult1);
            // Direct grade (P2: D2) is Protein; Cross (P2: D1) is Protein
            actualGradingResults.A_2.ShouldBeEquivalentTo(expectedGradingResult2);
        }

        [Test]
        public void CalculateGrades_SameMatchAndMismatchInDirect_SameMatchAndMismatchInCross_ReturnsSameMatchAndMismatchInBothOrientations()
        {
            var directMatchingSerology = new SerologyEntry("matching-split", SerologySubtype.Split, true);

            var patientResult1 = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithMatchingSerologies(new[] { directMatchingSerology })
                    .Build())
                .Build();

            var patientResult2 = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithMatchingSerologies(new[] { directMatchingSerology })
                    .Build())
                .Build();

            var donorResult1 = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithMatchingSerologies(new[] { new SerologyEntry("mismatched-serology", SerologySubtype.NotSplit, true) })
                    .Build())
                .Build();

            var donorResult2 = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithMatchingSerologies(new[] { directMatchingSerology })
                    .Build())
                .Build();

            var patientLookupResults = BuildLookupResultsAtMatchedLocus(patientResult1, patientResult2);
            var donorLookupResults = BuildLookupResultsAtMatchedLocus(donorResult1, donorResult2);
            var actualGradingResults = gradingService.CalculateGrades(patientLookupResults, donorLookupResults);

            var expectedMatchOrientations = new[] { MatchOrientation.Direct, MatchOrientation.Cross };
            var expectedGradingResult1 = new MatchGradeResult(MatchGrade.Split, expectedMatchOrientations);
            var expectedGradingResult2 = new MatchGradeResult(MatchGrade.Mismatch, expectedMatchOrientations);

            // Direct grade (P1: D1) is Mismatch; Cross (P1: D2) is Split
            actualGradingResults.A_1.ShouldBeEquivalentTo(expectedGradingResult1);
            // Direct grade (P2: D2) is Split; Cross (P2: D1) is Mismatch
            actualGradingResults.A_2.ShouldBeEquivalentTo(expectedGradingResult2);
        }

        [Test]
        public void CalculateGrades_TwoMismatchesInDirect_TwoMismatchesInCross_ReturnTwoMismatchesInBothOrientations()
        {
            var patientResult1 = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithMatchingPGroup("patient-p-group-1")
                    .Build())
                .Build();

            var patientResult2 = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithMatchingPGroup("patient-p-group-2")
                    .Build())
                .Build();

            var donorResult1 = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithMatchingPGroup("donor-p-group-1")
                    .Build())
                .Build();

            var donorResult2 = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithMatchingPGroup("donor-p-group-2")
                    .Build())
                .Build();

            var patientLookupResults = BuildLookupResultsAtMatchedLocus(patientResult1, patientResult2);
            var donorLookupResults = BuildLookupResultsAtMatchedLocus(donorResult1, donorResult2);
            var actualGradingResults = gradingService.CalculateGrades(patientLookupResults, donorLookupResults);

            var expectedMatchOrientations = new[] { MatchOrientation.Direct, MatchOrientation.Cross };
            var expectedGradingResult1 = new MatchGradeResult(MatchGrade.Mismatch, expectedMatchOrientations);
            var expectedGradingResult2 = new MatchGradeResult(MatchGrade.Mismatch, expectedMatchOrientations);

            // Direct grade (P1: D1) is Mismatch; Cross (P1: D2) is Mismatch
            actualGradingResults.A_1.ShouldBeEquivalentTo(expectedGradingResult1);
            // Direct grade (P2: D2) is Mismatch; Cross (P2: D1) is Mismatch
            actualGradingResults.A_2.ShouldBeEquivalentTo(expectedGradingResult2);
        }

        [Test]
        public void CalculateGrades_PatientIsMissingTheLocusTyping_ReturnsTwoPGroupMatchesInBothOrientations()
        {
            var patientLookupResults = BuildLookupResultsAtMatchedLocus(null, null);

            var donorLookupResults = BuildLookupResultsAtMatchedLocus(
                new HlaScoringLookupResultBuilder().Build(),
                new HlaScoringLookupResultBuilder().Build());

            var actualGradingResults = gradingService.CalculateGrades(patientLookupResults, donorLookupResults);

            var expectedMatchOrientations = new[] { MatchOrientation.Direct, MatchOrientation.Cross };
            var expectedGradingResult1 = new MatchGradeResult(MatchGrade.PGroup, expectedMatchOrientations);
            var expectedGradingResult2 = new MatchGradeResult(MatchGrade.PGroup, expectedMatchOrientations);

            actualGradingResults.A_1.ShouldBeEquivalentTo(expectedGradingResult1);
            actualGradingResults.A_2.ShouldBeEquivalentTo(expectedGradingResult2);
        }

        [Test]
        public void CalculateGrades_DonorIsMissingTheLocusTyping_ReturnsTwoPGroupMatchesInBothOrientations()
        {
            var patientLookupResults = BuildLookupResultsAtMatchedLocus(
                new HlaScoringLookupResultBuilder().Build(),
                new HlaScoringLookupResultBuilder().Build());

            var donorLookupResults = BuildLookupResultsAtMatchedLocus(null, null);

            var actualGradingResults = gradingService.CalculateGrades(patientLookupResults, donorLookupResults);

            var expectedMatchOrientations = new[] { MatchOrientation.Direct, MatchOrientation.Cross };
            var expectedGradingResult1 = new MatchGradeResult(MatchGrade.PGroup, expectedMatchOrientations);
            var expectedGradingResult2 = new MatchGradeResult(MatchGrade.PGroup, expectedMatchOrientations);

            actualGradingResults.A_1.ShouldBeEquivalentTo(expectedGradingResult1);
            actualGradingResults.A_2.ShouldBeEquivalentTo(expectedGradingResult2);
        }

        [Test]
        public void CalculateGrades_PatientAndDonorAreBothMissingTheLocusTyping_ReturnsTwoPGroupMatchesInBothOrientations()
        {
            var patientLookupResults = BuildLookupResultsAtMatchedLocus(null, null);

            var donorLookupResults = BuildLookupResultsAtMatchedLocus(null, null);

            var actualGradingResults = gradingService.CalculateGrades(patientLookupResults, donorLookupResults);

            var expectedMatchOrientations = new[] { MatchOrientation.Direct, MatchOrientation.Cross };
            var expectedGradingResult1 = new MatchGradeResult(MatchGrade.PGroup, expectedMatchOrientations);
            var expectedGradingResult2 = new MatchGradeResult(MatchGrade.PGroup, expectedMatchOrientations);

            actualGradingResults.A_1.ShouldBeEquivalentTo(expectedGradingResult1);
            actualGradingResults.A_2.ShouldBeEquivalentTo(expectedGradingResult2);
        }

        #endregion

        #region Tests: Typing affects the maximum possible grade assigned

        [Test]
        public void CalculateGrades_PatientIsSingleAllele_DonorIsSingleAllele_ReturnsMaxGradeOfGDna()
        {

        }

        [Test]
        public void CalculateGrades_PatientIsSingleAllele_DonorIsMultipleAllele_ReturnsMaxGradeOfGDna()
        {

        }

        [Test]
        public void CalculateGrades_PatientIsSingleAllele_DonorIsConsolidatedMolecular_ReturnsMaxGradeOfGGroup()
        {

        }

        [Test]
        public void CalculateGrades_PatientIsSingleAllele_DonorIsSerology_ReturnsMaxGradeOfAssociated()
        {

        }

        [Test]
        public void CalculateGrades_PatientIsMultipleAllele_DonorIsSingleAllele_ReturnsMaxGradeOfGDna()
        {

        }

        [Test]
        public void CalculateGrades_PatientIsMultipleAllele_DonorIsMultipleAllele_ReturnsMaxGradeOfGDna()
        {

        }

        [Test]
        public void CalculateGrades_PatientIsMultipleAllele_DonorIsConsolidatedMolecular_ReturnsMaxGradeOfGGroup()
        {

        }

        [Test]
        public void CalculateGrades_PatientIsMultipleAllele_DonorIsSerology_ReturnsMaxGradeOfAssociated()
        {

        }

        [Test]
        public void CalculateGrades_PatientIsConsolidatedMolecular_DonorIsSingleAllele_ReturnsMaxGradeOfGGroup()
        {

        }

        [Test]
        public void CalculateGrades_PatientIsConsolidatedMolecular_DonorIsMultipleAllele_ReturnsMaxGradeOfGGroup()
        {

        }

        [Test]
        public void CalculateGrades_PatientIsConsolidatedMolecular_DonorIsConsolidatedMolecular_ReturnsMaxGradeOfGGroup()
        {

        }

        [Test]
        public void CalculateGrades_PatientIsConsolidatedMolecular_DonorIsSerology_ReturnsMaxGradeOfAssociated()
        {

        }

        [Test]
        public void CalculateGrades_PatientIsSerology_DonorIsSingleAllele_ReturnsMaxGradeOfAssociated()
        {

        }

        [Test]
        public void CalculateGrades_PatientIsSerology_DonorIsMultipleAllele_ReturnsMaxGradeOfAssociated()
        {

        }

        [Test]
        public void CalculateGrades_PatientIsSerology_DonorIsConsolidatedMolecular_ReturnsMaxGradeOfAssociated()
        {

        }

        [Test]
        public void CalculateGrades_PatientIsSerology_DonorIsSerology_ReturnsMaxGradeOfAssociated()
        {

        }

        #endregion

        private static PhenotypeInfo<IHlaScoringLookupResult> BuildLookupResultsAtMatchedLocus(
            IHlaScoringLookupResult positionOneResult,
            IHlaScoringLookupResult positionTwoResult)
        {
            var donorLookupResults = new PhenotypeInfo<IHlaScoringLookupResult>();
            donorLookupResults.SetAtLocus(MatchedLocus, TypePositions.One, positionOneResult);
            donorLookupResults.SetAtLocus(MatchedLocus, TypePositions.Two, positionTwoResult);

            return donorLookupResults;
        }
    }
}
