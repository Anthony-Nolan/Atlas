using Atlas.Client.Models.Search.Results.Matching.PerLocus;
using Atlas.Client.Models.Search.Results.MatchPrediction;
using Atlas.Functions.Services.MatchCategories;
using Atlas.Functions.Test.Builders;
using FluentAssertions;
using NUnit.Framework;
using LocusMatchCategories = Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.LocusInfo<Atlas.Client.Models.Search.Results.MatchPrediction.PredictiveMatchCategory?>;

namespace Atlas.Functions.Test.Services
{
    [TestFixture]
    public class PositionalMatchCategoryOrientatorTests
    {
        #region TestData

        private const PredictiveMatchCategory NonMismatchCategory = PredictiveMatchCategory.Exact;
        private const PredictiveMatchCategory MismatchCategory = PredictiveMatchCategory.Mismatch;

        private static readonly LocusMatchCategories BothCategoriesNotMismatch = new(NonMismatchCategory, NonMismatchCategory);
        private static readonly LocusMatchCategories BothCategoriesMismatch = new(MismatchCategory, MismatchCategory);
        private static readonly LocusMatchCategories MismatchCategoryInOne = new(MismatchCategory, NonMismatchCategory);
        private static readonly LocusMatchCategories MismatchCategoryInTwo = new(NonMismatchCategory, MismatchCategory);
        private static readonly List<LocusMatchCategories> AllCategoryCombinations = new() { BothCategoriesNotMismatch, BothCategoriesMismatch, MismatchCategoryInOne, MismatchCategoryInTwo };
        private static readonly List<LocusMatchCategories> OnePositionHasMismatchCategory = new() { MismatchCategoryInOne, MismatchCategoryInTwo };

        private static readonly LocusSearchResult BothGradesAreMismatch = LocusScoreResultBuilder.New.WithMatchGradesAtBothPositions(LocusMatchCategory.Mismatch, MatchGrade.Mismatch);
        private static readonly LocusSearchResult BothGradesAreMatch = LocusScoreResultBuilder.New.WithMatchGradesAtBothPositions(LocusMatchCategory.Match, MatchGrade.PGroup);
        private static readonly LocusSearchResult MismatchGradeInOne = LocusScoreResultBuilder.New.WithMatchGrades(LocusMatchCategory.Mismatch, MatchGrade.Mismatch, MatchGrade.PGroup);
        private static readonly LocusSearchResult MismatchGradeInTwo = LocusScoreResultBuilder.New.WithMatchGrades(LocusMatchCategory.Mismatch, MatchGrade.PGroup, MatchGrade.Mismatch);
        private static readonly List<LocusSearchResult> AllScoreResults = new() { BothGradesAreMismatch, BothGradesAreMatch, MismatchGradeInOne, MismatchGradeInTwo };
        private static readonly List<LocusSearchResult> MismatchScoreResults = new() { BothGradesAreMismatch, MismatchGradeInOne, MismatchGradeInTwo };
        
        #endregion

        private IPositionalMatchCategoryOrientator orientator;

        [SetUp]
        public void SetUp()
        {
            orientator = new PositionalMatchCategoryOrientator();
        }

        [Test]
        public void AlignCategoriesToMismatchScore_NoMatchCategories_ReturnsNull(
            [ValueSource(nameof(AllScoreResults))] LocusSearchResult score)
        {
            var returnedCategories = orientator.AlignCategoriesToMismatchScore(null, score);

            returnedCategories.Should().BeNull();
        }

        [Test]
        public void AlignCategoriesToMismatchScore_EachMatchCategoryIsNull_ReturnsOriginalMatchCategories(
            [ValueSource(nameof(AllScoreResults))] LocusSearchResult score)
        {
            var returnedCategories = orientator.AlignCategoriesToMismatchScore(new LocusMatchCategories(), score);

            returnedCategories.Should().NotBeNull();
            returnedCategories.Position1.Should().BeNull();
            returnedCategories.Position2.Should().BeNull();
        }

        [Test]
        public void AlignCategoriesToMismatchScore_NoLocusScore_ReturnsOriginalMatchCategories(
            [ValueSource(nameof(AllCategoryCombinations))] LocusMatchCategories inputCategories)
        {
            var returnedCategories = orientator.AlignCategoriesToMismatchScore(inputCategories, null);

            returnedCategories.Position1.Should().Be(inputCategories.Position1);
            returnedCategories.Position2.Should().Be(inputCategories.Position2);
        }

        [Test]
        public void AlignCategoriesToMismatchScore_ScoreCategoryIsNull_ReturnsOriginalMatchCategories(
            [ValueSource(nameof(AllCategoryCombinations))] LocusMatchCategories inputCategories)
        {
            var returnedCategories = orientator.AlignCategoriesToMismatchScore(inputCategories, new LocusSearchResult());

            returnedCategories.Position1.Should().Be(inputCategories.Position1);
            returnedCategories.Position2.Should().Be(inputCategories.Position2);
        }

        [Test]
        public void AlignCategoriesToMismatchScore_ScoreCategoryIsMatch_ReturnsOriginalMatchCategories(
            [ValueSource(nameof(AllCategoryCombinations))] LocusMatchCategories inputCategories)
        {
            var returnedCategories = orientator.AlignCategoriesToMismatchScore(inputCategories, BothGradesAreMatch);

            returnedCategories.Position1.Should().Be(inputCategories.Position1);
            returnedCategories.Position2.Should().Be(inputCategories.Position2);
        }

        [Test]
        public void AlignCategoriesToMismatchScore_BothPredictiveCategoriesAreNotMismatch_ReturnsOriginalMatchCategories(
            [ValueSource(nameof(MismatchScoreResults))] LocusSearchResult mismatchScore)
        {
            var returnedCategories = orientator.AlignCategoriesToMismatchScore(BothCategoriesNotMismatch, mismatchScore);

            returnedCategories.Position1.Should().Be(NonMismatchCategory);
            returnedCategories.Position2.Should().Be(NonMismatchCategory);
        }

        [Test]
        public void AlignCategoriesToMismatchScore_BothPredictiveCategoriesAreMismatch_ReturnsOriginalMatchCategories(
            [ValueSource(nameof(MismatchScoreResults))] LocusSearchResult mismatchScore)
        {
            var returnedCategories = orientator.AlignCategoriesToMismatchScore(BothCategoriesMismatch, mismatchScore);

            returnedCategories.Position1.Should().Be(MismatchCategory);
            returnedCategories.Position2.Should().Be(MismatchCategory);
        }

        [Test]
        public void AlignCategoriesToMismatchScore_NoScoreDetails_ReturnsOriginalMatchCategories(
            [ValueSource(nameof(OnePositionHasMismatchCategory))] LocusMatchCategories inputCategories)
        {
            var noScoreDetails = LocusScoreResultBuilder.New.With(x => x.MatchCategory, LocusMatchCategory.Mismatch);

            var returnedCategories = orientator.AlignCategoriesToMismatchScore(inputCategories, noScoreDetails);

            returnedCategories.Position1.Should().Be(inputCategories.Position1);
            returnedCategories.Position2.Should().Be(inputCategories.Position2);
        }

        [Test]
        public void AlignCategoriesToMismatchScore_BothMatchGradesAreMismatch_ReturnsOriginalMatchCategories(
            [ValueSource(nameof(OnePositionHasMismatchCategory))] LocusMatchCategories inputCategories)
        {
            var returnedCategories = orientator.AlignCategoriesToMismatchScore(inputCategories, BothGradesAreMismatch);

            returnedCategories.Position1.Should().Be(inputCategories.Position1);
            returnedCategories.Position2.Should().Be(inputCategories.Position2);
        }

        [Test]
        public void AlignCategoriesToMismatchScore_BothPredictedMismatchAndScoreMismatchInPositionOne_ReturnsOriginalMatchCategories()
        {
            var returnedCategories = orientator.AlignCategoriesToMismatchScore(MismatchCategoryInOne, MismatchGradeInOne);

            returnedCategories.Position1.Should().Be(MismatchCategory);
            returnedCategories.Position2.Should().Be(NonMismatchCategory);
        }

        [Test]
        public void AlignCategoriesToMismatchScore_BothPredictedMismatchAndScoreMismatchInPositionTwo_ReturnsOriginalMatchCategories()
        {
            var returnedCategories = orientator.AlignCategoriesToMismatchScore(MismatchCategoryInTwo, MismatchGradeInTwo);

            returnedCategories.Position1.Should().Be(NonMismatchCategory);
            returnedCategories.Position2.Should().Be(MismatchCategory);
        }

        [Test]
        public void AlignCategoriesToMismatchScore_PredictedMismatchInOneAndScoreMismatchInTwo_AlignsCategoriesToScore()
        {
            var returnedCategories = orientator.AlignCategoriesToMismatchScore(MismatchCategoryInOne, MismatchGradeInTwo);

            returnedCategories.Position1.Should().Be(NonMismatchCategory);
            returnedCategories.Position2.Should().Be(MismatchCategory);
        }

        [Test]
        public void AlignCategoriesToMismatchScore_PredictedMismatchInTwoAndScoreMismatchInOne_AlignsCategoriesToScore()
        {
            var returnedCategories = orientator.AlignCategoriesToMismatchScore(MismatchCategoryInTwo, MismatchGradeInOne);

            returnedCategories.Position1.Should().Be(MismatchCategory);
            returnedCategories.Position2.Should().Be(NonMismatchCategory);
        }
    }
}