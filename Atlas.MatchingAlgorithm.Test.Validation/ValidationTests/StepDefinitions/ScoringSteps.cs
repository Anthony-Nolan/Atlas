using System;
using System.Collections.Generic;
using System.Linq;
using Atlas.Client.Models.Search.Results.Matching;
using Atlas.Client.Models.Search.Results.Matching.PerLocus;
using Atlas.Common.GeneticData;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.TransferModels;
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
                AssertMatchGrade(
                    expectedPosition,
                    donorResult.ScoringResult.ScoringResultsByLocus.ToLociInfo().GetLocus(expectedLocus),
                    validMatchGrades
                );
            }
        }

        [Then("the locus match category should be (.*) at (.*)")]
        public void ThenTheLocusMatchCategoryShouldBe(string category, string locus)
        {
            var donorResult = GetSearchResultForSingleDonor();
            var expectedLocusMatchCategory = ParseExpectedLocusMatchCategory(category);
            var expectedLoci = ParseExpectedLoci(locus);

            foreach (var expectedLocus in expectedLoci)
            {
                switch (expectedLocus)
                {
                    case Locus.A:
                        donorResult.ScoringResult.ScoringResultsByLocus.A.MatchCategory.Should().Be(expectedLocusMatchCategory);
                        break;
                    case Locus.B:
                        donorResult.ScoringResult.ScoringResultsByLocus.B.MatchCategory.Should().Be(expectedLocusMatchCategory);
                        break;
                    case Locus.C:
                        donorResult.ScoringResult.ScoringResultsByLocus.C.MatchCategory.Should().Be(expectedLocusMatchCategory);
                        break;
                    case Locus.Dpb1:
                        donorResult.ScoringResult.ScoringResultsByLocus.Dpb1.MatchCategory.Should().Be(expectedLocusMatchCategory);
                        break;
                    case Locus.Dqb1:
                        donorResult.ScoringResult.ScoringResultsByLocus.Dqb1.MatchCategory.Should().Be(expectedLocusMatchCategory);
                        break;
                    case Locus.Drb1:
                        donorResult.ScoringResult.ScoringResultsByLocus.Drb1.MatchCategory.Should().Be(expectedLocusMatchCategory);
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

            var results = apiResult.Results.Results.ToList();

            var higherResult = ParseResultType(results, higherResultType)?.AtlasDonorId;
            var lowerResult = ParseResultType(results, lowerResultType)?.AtlasDonorId;

            results.Select(r => r.AtlasDonorId).Should().ContainInOrder(new List<int?> {higherResult, lowerResult});
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
                        AssertMatchConfidence(expectedPositions, donorResult.ScoringResult.ScoringResultsByLocus.A, validMatchConfidence);
                        break;
                    case Locus.B:
                        AssertMatchConfidence(expectedPositions, donorResult.ScoringResult.ScoringResultsByLocus.B, validMatchConfidence);
                        break;
                    case Locus.C:
                        AssertMatchConfidence(expectedPositions, donorResult.ScoringResult.ScoringResultsByLocus.C, validMatchConfidence);
                        break;
                    case Locus.Dpb1:
                        AssertMatchConfidence(expectedPositions, donorResult.ScoringResult.ScoringResultsByLocus.Dpb1, validMatchConfidence);
                        break;
                    case Locus.Dqb1:
                        AssertMatchConfidence(expectedPositions, donorResult.ScoringResult.ScoringResultsByLocus.Dqb1, validMatchConfidence);
                        break;
                    case Locus.Drb1:
                        AssertMatchConfidence(expectedPositions, donorResult.ScoringResult.ScoringResultsByLocus.Drb1, validMatchConfidence);
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
                .Results
                .Single(r => r.AtlasDonorId == expectedDonorProvider.GetExpectedMatchingDonorIds().Single());
        }

        private IEnumerable<MatchGrade> ParseExpectedMatchGrades(string grades)
        {
            switch (grades.ToLower())
            {
                case "p-group":
                    return new[] {MatchGrade.PGroup};
                case "g-group":
                    return new[] {MatchGrade.GGroup};
                case "cdna":
                    return new[] {MatchGrade.CDna};
                case "gdna":
                    return new[] {MatchGrade.GDna};
                case "protein":
                    return new[] {MatchGrade.Protein};
                case "serology":
                    return new[] {MatchGrade.Associated, MatchGrade.Broad, MatchGrade.Split};
                case "mismatch":
                    return new[] {MatchGrade.Mismatch};
                case "unknown":
                    return new[] {MatchGrade.Unknown};
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
            var allLoci = new[] {Locus.A, Locus.B, Locus.C, Locus.Dpb1, Locus.Dqb1, Locus.Drb1};
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
                    return new LocusPosition?[] {LocusPosition.One, LocusPosition.Two};
                case "position 1":
                    return new LocusPosition?[] {LocusPosition.One};
                case "position 2":
                    return new LocusPosition?[] {LocusPosition.Two};
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
                    return results.Find(r => r.ScoringResult.ScoringResultsByLocus.Dqb1.MatchCount == 2);
                case "a mismatch at DQB1":
                    return results.Find(r => r.ScoringResult.ScoringResultsByLocus.Dqb1.MatchCount < 2);
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
                    return results.Find(r => IsOneOfMatchGradesAtMatchedLoci(r, new[] {MatchGrade.Broad, MatchGrade.Associated, MatchGrade.Split}));
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
            return IsOneOfMatchGradesAtMatchedLoci(result, new[] {matchGrade});
        }

        private static bool IsMatchConfidenceAtMatchedLoci(MatchingAlgorithmResult result, MatchConfidence matchConfidence)
        {
            return IsOneOfMatchConfidencesAtMatchedLoci(result, new[] {matchConfidence});
        }

        private static bool IsOneOfMatchGradesAtMatchedLoci(MatchingAlgorithmResult result, IEnumerable<MatchGrade> matchGrades)
        {
            var positionResults = new[]
            {
                result.ScoringResult.ScoringResultsByLocus.A,
                result.ScoringResult.ScoringResultsByLocus.A,
                result.ScoringResult.ScoringResultsByLocus.B,
                result.ScoringResult.ScoringResultsByLocus.B,
                result.ScoringResult.ScoringResultsByLocus.C,
                result.ScoringResult.ScoringResultsByLocus.C,
                result.ScoringResult.ScoringResultsByLocus.Dqb1,
                result.ScoringResult.ScoringResultsByLocus.Dqb1,
                result.ScoringResult.ScoringResultsByLocus.Drb1,
                result.ScoringResult.ScoringResultsByLocus.Drb1,
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
                result.ScoringResult.ScoringResultsByLocus.A,
                result.ScoringResult.ScoringResultsByLocus.A,
                result.ScoringResult.ScoringResultsByLocus.B,
                result.ScoringResult.ScoringResultsByLocus.B,
                result.ScoringResult.ScoringResultsByLocus.C,
                result.ScoringResult.ScoringResultsByLocus.C,
                result.ScoringResult.ScoringResultsByLocus.Dqb1,
                result.ScoringResult.ScoringResultsByLocus.Dqb1,
                result.ScoringResult.ScoringResultsByLocus.Drb1,
                result.ScoringResult.ScoringResultsByLocus.Drb1,
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
                matchingAlgorithmResult.ScoringResult.ScoringResultsByLocus.A.IsLocusMatchCountIncludedInTotal,
                matchingAlgorithmResult.ScoringResult.ScoringResultsByLocus.B.IsLocusMatchCountIncludedInTotal,
                matchingAlgorithmResult.ScoringResult.ScoringResultsByLocus.C.IsLocusMatchCountIncludedInTotal,
                matchingAlgorithmResult.ScoringResult.ScoringResultsByLocus.Dqb1.IsLocusMatchCountIncludedInTotal,
                matchingAlgorithmResult.ScoringResult.ScoringResultsByLocus.Drb1.IsLocusMatchCountIncludedInTotal,
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

        private LocusMatchCategory? ParseExpectedLocusMatchCategory(string category)
        {
            switch (category)
            {
                case "Match":
                    return LocusMatchCategory.Match;
                case "Mismatch":
                    return LocusMatchCategory.Mismatch;
                case "Unknown":
                    return LocusMatchCategory.Unknown;
                case "PermissiveMismatch":
                case "Permissive Mismatch":
                    return LocusMatchCategory.PermissiveMismatch;
                default:
                    scenarioContext.Pending();
                    return null;
            }
        }
    }
}