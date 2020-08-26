using System;
using System.Collections.Generic;
using System.Linq;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Client.Models.Search.Results.Matching.PerLocus;
using Atlas.Common.GeneticData;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Models;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Services;
using FluentAssertions;
using TechTalk.SpecFlow;

namespace Atlas.MatchingAlgorithm.Test.Validation.ValidationTests.StepDefinitions
{
    [Binding]
    public class ScoringSteps
    {
        private readonly ScenarioContext scenarioContext;

        public ScoringSteps(ScenarioContext scenarioContext)
        {
            this.scenarioContext = scenarioContext;
        }

        [Then("the match grade should be (.*) at (.*) at (.*)")]
        public void ThenTheMatchGradeShouldBe(string grade, string locus, string position)
        {
            var donorResult = GetSearchResultForSingleDonor();
            var validMatchGrades = ParseExpectedMatchGrades(grade).ToList();
            var expectedLoci = ParseExpectedLoci(locus);
            var expectedPosition = ParseExpectedPositions(position);

            foreach (var expectedLocus in expectedLoci)
            {
                switch (expectedLocus)
                {
                    case Locus.A:
                        AssertMatchGrade(expectedPosition, donorResult.ScoringResult.SearchResultsByLocus.A, validMatchGrades);
                        break;
                    case Locus.B:
                        AssertMatchGrade(expectedPosition, donorResult.ScoringResult.SearchResultsByLocus.B, validMatchGrades);
                        break;
                    case Locus.C:
                        AssertMatchGrade(expectedPosition, donorResult.ScoringResult.SearchResultsByLocus.C, validMatchGrades);
                        break;
                    case Locus.Dpb1:
                        AssertMatchGrade(expectedPosition, donorResult.ScoringResult.SearchResultsByLocus.Dpb1, validMatchGrades);
                        break;
                    case Locus.Dqb1:
                        AssertMatchGrade(expectedPosition, donorResult.ScoringResult.SearchResultsByLocus.Dqb1, validMatchGrades);
                        break;
                    case Locus.Drb1:
                        AssertMatchGrade(expectedPosition, donorResult.ScoringResult.SearchResultsByLocus.Drb1, validMatchGrades);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        [Then("the match category should be (.*)")]
        public void ThenTheMatchCategoryShouldBe(string category)
        {
            var donorResult = GetSearchResultForSingleDonor();
            var expectedMatchCategory = ParseExpectedMatchCategory(category);

            donorResult.ScoringResult.MatchCategory.Should().Be(expectedMatchCategory);
        }

        [Then("(.*) should be returned above (.*)")]
        public void ThenXShouldBeReturnedAboveY(string higherResultType, string lowerResultType)
        {
            var apiResult = scenarioContext.Get<SearchAlgorithmApiResult>();
            apiResult.IsSuccess.Should().BeTrue();

            var results = apiResult.Results.MatchingAlgorithmResults.ToList();

            var higherResult = ParseResultType(results, higherResultType);
            var lowerResult = ParseResultType(results, lowerResultType);

            results.Should().ContainInOrder(new List<MatchingAlgorithmResult> { higherResult, lowerResult });
        }

        [Then(@"the match confidence should be (.*) at (.*) at (.*)")]
        public void ThenTheMatchConfidenceShouldBe(string confidence, string locus, string position)
        {
            var donorResult = GetSearchResultForSingleDonor();
            var validMatchConfidence = ParseExpectedMatchConfidence(confidence);
            var expectedLoci = ParseExpectedLoci(locus);
            var expectedPositions = ParseExpectedPositions(position);

            foreach (var expectedLocus in expectedLoci)
            {
                switch (expectedLocus)
                {
                    case Locus.A:
                        AssertMatchConfidence(expectedPositions, donorResult.ScoringResult.SearchResultsByLocus.A, validMatchConfidence);
                        break;
                    case Locus.B:
                        AssertMatchConfidence(expectedPositions, donorResult.ScoringResult.SearchResultsByLocus.B, validMatchConfidence);
                        break;
                    case Locus.C:
                        AssertMatchConfidence(expectedPositions, donorResult.ScoringResult.SearchResultsByLocus.C, validMatchConfidence);
                        break;
                    case Locus.Dpb1:
                        AssertMatchConfidence(expectedPositions, donorResult.ScoringResult.SearchResultsByLocus.Dpb1, validMatchConfidence);
                        break;
                    case Locus.Dqb1:
                        AssertMatchConfidence(expectedPositions, donorResult.ScoringResult.SearchResultsByLocus.Dqb1, validMatchConfidence);
                        break;
                    case Locus.Drb1:
                        AssertMatchConfidence(expectedPositions, donorResult.ScoringResult.SearchResultsByLocus.Drb1, validMatchConfidence);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        [Then(@"the typed loci count should be (.*)")]
        public void ThenTheMatchConfidenceShouldBe(int typedLociCount)
        {
            var donorResult = GetSearchResultForSingleDonor();
            donorResult.ScoringResult.TypedLociCountAtScoredLoci.Should().Be(typedLociCount);
        }

        private MatchingAlgorithmResult GetSearchResultForSingleDonor()
        {
            var expectedDonorProvider = scenarioContext.Get<IExpectedDonorProvider>();
            var apiResult = scenarioContext.Get<SearchAlgorithmApiResult>();
            apiResult.IsSuccess.Should().BeTrue();

            return apiResult
                .Results
                .MatchingAlgorithmResults
                .Single(r => r.AtlasDonorId == expectedDonorProvider.GetExpectedMatchingDonorIds().Single());
        }

        private IEnumerable<MatchGrade> ParseExpectedMatchGrades(string grades)
        {
            switch (grades.ToLower())
            {
                case "p-group":
                    return new[] { MatchGrade.PGroup };
                case "g-group":
                    return new[] { MatchGrade.GGroup };
                case "cdna":
                    return new[] { MatchGrade.CDna };
                case "gdna":
                    return new[] { MatchGrade.GDna };
                case "protein":
                    return new[] { MatchGrade.Protein };
                case "serology":
                    return new[] { MatchGrade.Associated, MatchGrade.Broad, MatchGrade.Split };
                case "permissive mismatch":
                    return new[] { MatchGrade.PermissiveMismatch };
                case "mismatch":
                    return new[] { MatchGrade.Mismatch };
                default:
                    scenarioContext.Pending();
                    return new List<MatchGrade>();
            }
        }

        private MatchConfidence? ParseExpectedMatchConfidence(string confidence)
        {
            switch (confidence)
            {
                case "Definite":
                    return MatchConfidence.Definite;
                case "Exact":
                    return MatchConfidence.Exact;
                case "Potential":
                    return MatchConfidence.Potential;
                case "Mismatch":
                    return MatchConfidence.Mismatch;
                default:
                    scenarioContext.Pending();
                    return null;
            }
        }

        private MatchCategory? ParseExpectedMatchCategory(string category)
        {
            switch (category)
            {
                case "Definite":
                    return MatchCategory.Definite;
                case "Exact":
                    return MatchCategory.Exact;
                case "Potential":
                    return MatchCategory.Potential;
                case "Mismatch":
                    return MatchCategory.Mismatch;
                case "PermissiveMismatch":
                case "Permissive Mismatch":
                    return MatchCategory.PermissiveMismatch;
                default:
                    scenarioContext.Pending();
                    return null;
            }
        }

        private IEnumerable<Locus> ParseExpectedLoci(string locus)
        {
            var allLoci = new[] { Locus.A, Locus.B, Locus.C, Locus.Dpb1, Locus.Dqb1, Locus.Drb1 };
            var expectedLoci = new List<Locus>();

            switch (locus.ToUpper())
            {
                case "ALL LOCI":
                case "EACH LOCUS":
                    return allLoci;
                case "ALL LOCI EXCEPT DPB1":
                    return allLoci.Where(l => l != Locus.Dpb1);
                case "LOCUS A":
                case "A":
                    expectedLoci.Add(Locus.A);
                    break;
                case "LOCUS B":
                case "B":
                    expectedLoci.Add(Locus.B);
                    break;
                case "LOCUS C":
                case "C":
                    expectedLoci.Add(Locus.C);
                    break;
                case "LOCUS DPB1":
                case "DPB1":
                    expectedLoci.Add(Locus.Dpb1);
                    break;
                case "LOCUS DQB1":
                case "DQB1":
                    expectedLoci.Add(Locus.Dqb1);
                    break;
                case "LOCUS DRB1":
                case "DRB1":
                    expectedLoci.Add(Locus.Drb1);
                    break;
                default:
                    scenarioContext.Pending();
                    break;
            }

            return expectedLoci;
        }

        private LocusPosition?[] ParseExpectedPositions(string position)
        {
            switch (position)
            {
                case "both positions":
                    return new LocusPosition?[] { LocusPosition.One, LocusPosition.Two };
                case "position 1":
                    return new LocusPosition?[] { LocusPosition.One };
                case "position 2":
                    return new LocusPosition?[] { LocusPosition.Two };
                default:
                    scenarioContext.Pending();
                    return null;
            }
        }

        private MatchingAlgorithmResult ParseResultType(List<MatchingAlgorithmResult> results, string resultType)
        {
            switch (resultType)
            {
                case "an 8/8 result":
                    return results.Find(r => r.MatchingResult.TotalMatchCount == 8 && NumberOfLociSearched(r) == 4);
                case "a 7/8 result":
                    return results.Find(r => r.MatchingResult.TotalMatchCount == 7 && NumberOfLociSearched(r) == 4);
                case "a 6/8 result":
                    return results.Find(r => r.MatchingResult.TotalMatchCount == 6 && NumberOfLociSearched(r) == 4);
                case "a 5/8 result":
                    return results.Find(r => r.MatchingResult.TotalMatchCount == 5 && NumberOfLociSearched(r) == 4);
                case "a 4/8 result":
                    return results.Find(r => r.MatchingResult.TotalMatchCount == 4 && NumberOfLociSearched(r) == 4);
                case "a match at DQB1":
                    return results.Find(r => r.ScoringResult.SearchResultsByLocus.Dqb1.MatchCount == 2);
                case "a mismatch at DQB1":
                    return results.Find(r => r.ScoringResult.SearchResultsByLocus.Dqb1.MatchCount < 2);
                case "a full gDNA match":
                    return results.Find(r => IsMatchGradeAtMatchedLoci(r, MatchGrade.GDna));
                case "a full cDNA match":
                    return results.Find(r => IsMatchGradeAtMatchedLoci(r, MatchGrade.CDna));
                case "a full protein match":
                    return results.Find(r => IsMatchGradeAtMatchedLoci(r, MatchGrade.Protein));
                case "a full g-group match":
                    return results.Find(r => IsMatchGradeAtMatchedLoci(r, MatchGrade.GGroup));
                case "a full p-group match":
                    return results.Find(r => IsMatchGradeAtMatchedLoci(r, MatchGrade.PGroup));
                case "a full serology match":
                    return results.Find(r => IsOneOfMatchGradesAtMatchedLoci(r, new[] { MatchGrade.Broad, MatchGrade.Associated, MatchGrade.Split }));
                case "a full definite match":
                    return results.Find(r => IsMatchConfidenceAtMatchedLoci(r, MatchConfidence.Definite));
                case "a full exact match":
                    return results.Find(r => IsMatchConfidenceAtMatchedLoci(r, MatchConfidence.Exact));
                case "a full potential match":
                    return results.Find(r => IsMatchConfidenceAtMatchedLoci(r, MatchConfidence.Potential));
                default:
                    scenarioContext.Pending();
                    return null;
            }
        }

        private static bool IsMatchGradeAtMatchedLoci(MatchingAlgorithmResult result, MatchGrade matchGrade)
        {
            return IsOneOfMatchGradesAtMatchedLoci(result, new[] { matchGrade });
        }

        private static bool IsMatchConfidenceAtMatchedLoci(MatchingAlgorithmResult result, MatchConfidence matchConfidence)
        {
            return IsOneOfMatchConfidencesAtMatchedLoci(result, new[] { matchConfidence });
        }

        private static bool IsOneOfMatchGradesAtMatchedLoci(MatchingAlgorithmResult result, IEnumerable<MatchGrade> matchGrades)
        {
            var positionResults = new[]
            {
                result.ScoringResult.SearchResultsByLocus.A,
                result.ScoringResult.SearchResultsByLocus.A,
                result.ScoringResult.SearchResultsByLocus.B,
                result.ScoringResult.SearchResultsByLocus.B,
                result.ScoringResult.SearchResultsByLocus.C,
                result.ScoringResult.SearchResultsByLocus.C,
                result.ScoringResult.SearchResultsByLocus.Dqb1,
                result.ScoringResult.SearchResultsByLocus.Dqb1,
                result.ScoringResult.SearchResultsByLocus.Drb1,
                result.ScoringResult.SearchResultsByLocus.Drb1,
            };

            return positionResults.All(r =>
                    // we only want to assert grades of searched loci
                    !r.IsLocusMatchCountIncludedInTotal
                    || (matchGrades.Contains(r.ScoreDetailsAtPositionOne.MatchGrade) &&
                        matchGrades.Contains(r.ScoreDetailsAtPositionTwo.MatchGrade))
            );
        }

        private static bool IsOneOfMatchConfidencesAtMatchedLoci(MatchingAlgorithmResult result, IEnumerable<MatchConfidence> matchConfidences)
        {
            var positionResults = new[]
            {
                result.ScoringResult.SearchResultsByLocus.A,
                result.ScoringResult.SearchResultsByLocus.A,
                result.ScoringResult.SearchResultsByLocus.B,
                result.ScoringResult.SearchResultsByLocus.B,
                result.ScoringResult.SearchResultsByLocus.C,
                result.ScoringResult.SearchResultsByLocus.C,
                result.ScoringResult.SearchResultsByLocus.Dqb1,
                result.ScoringResult.SearchResultsByLocus.Dqb1,
                result.ScoringResult.SearchResultsByLocus.Drb1,
                result.ScoringResult.SearchResultsByLocus.Drb1,
            };

            return positionResults.All(r =>
                    // we only want to assert grades of searched loci
                    !r.IsLocusMatchCountIncludedInTotal
                    || (matchConfidences.Contains(r.ScoreDetailsAtPositionOne.MatchConfidence) &&
                        matchConfidences.Contains(r.ScoreDetailsAtPositionTwo.MatchConfidence))
            );
        }

        private static int NumberOfLociSearched(MatchingAlgorithmResult matchingAlgorithmResult)
        {
            var loci = new[]
            {
                matchingAlgorithmResult.ScoringResult.SearchResultsByLocus.A.IsLocusMatchCountIncludedInTotal,
                matchingAlgorithmResult.ScoringResult.SearchResultsByLocus.B.IsLocusMatchCountIncludedInTotal,
                matchingAlgorithmResult.ScoringResult.SearchResultsByLocus.C.IsLocusMatchCountIncludedInTotal,
                matchingAlgorithmResult.ScoringResult.SearchResultsByLocus.Dqb1.IsLocusMatchCountIncludedInTotal,
                matchingAlgorithmResult.ScoringResult.SearchResultsByLocus.Drb1.IsLocusMatchCountIncludedInTotal,
            };
            return loci.Count(x => x);
        }

        private static void AssertMatchGrade(
            LocusPosition?[] expectedPosition,
            LocusSearchResult locusSearchResult,
            IReadOnlyCollection<MatchGrade> validMatchGrades
        )
        {
            if (expectedPosition.Contains(LocusPosition.One))
            {
                validMatchGrades.Should().Contain(locusSearchResult.ScoreDetailsAtPositionOne.MatchGrade);
            }

            if (expectedPosition.Contains(LocusPosition.Two))
            {
                validMatchGrades.Should().Contain(locusSearchResult.ScoreDetailsAtPositionTwo.MatchGrade);
            }
        }

        private static void AssertMatchConfidence(
            LocusPosition?[] expectedPosition,
            LocusSearchResult locusSearchResult,
            MatchConfidence? validMatchConfidence
        )
        {
            if (expectedPosition.Contains(LocusPosition.One))
            {
                validMatchConfidence.Should().Be(locusSearchResult.ScoreDetailsAtPositionOne.MatchConfidence);
            }

            if (expectedPosition.Contains(LocusPosition.Two))
            {
                validMatchConfidence.Should().Be(locusSearchResult.ScoreDetailsAtPositionTwo.MatchConfidence);
            }
        }
    }
}