using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Nova.SearchAlgorithm.Client.Models.SearchResults;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Services.PatientDataSelection;
using TechTalk.SpecFlow;

namespace Nova.SearchAlgorithm.Test.Validation.ValidationTests.StepDefinitions
{
    [Binding]
    public class ScoringSteps
    {
        [Then("the match grade should be (.*) at (.*) at (.*)")]
        public void ThenTheMatchGradeShouldBe(string grade, string locus, string position)
        {
            var patientDataSelector = ScenarioContext.Current.Get<IPatientDataFactory>();
            var apiResult = ScenarioContext.Current.Get<SearchAlgorithmApiResult>();
            apiResult.IsSuccess.Should().BeTrue();
            var donorResult = apiResult.Results.SearchResults.Single(r => r.DonorId == patientDataSelector.GetExpectedMatchingDonorIds().Single());

            var validMatchGrades = ParseExpectedMatchGrades(grade).ToList();
            var expectedLoci = ParseExpectedLoci(locus);
            var expectedPosition = ParseExpectedPositions(position);

            foreach (var expectedLocus in expectedLoci)
            {
                switch (expectedLocus)
                {
                    case Locus.A:
                        AssertMatchGrade(expectedPosition, donorResult.SearchResultAtLocusA, validMatchGrades);
                        break;
                    case Locus.B:
                        AssertMatchGrade(expectedPosition, donorResult.SearchResultAtLocusB, validMatchGrades);
                        break;
                    case Locus.C:
                        AssertMatchGrade(expectedPosition, donorResult.SearchResultAtLocusC, validMatchGrades);
                        break;
                    case Locus.Dpb1:
                        ScenarioContext.Current.Pending();
                        break;
                    case Locus.Dqb1:
                        AssertMatchGrade(expectedPosition, donorResult.SearchResultAtLocusDqb1, validMatchGrades);
                        break;
                    case Locus.Drb1:
                        AssertMatchGrade(expectedPosition, donorResult.SearchResultAtLocusDrb1, validMatchGrades);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        [Then("(.*) should be returned above (.*)")]
        public void ThenXShouldBeReturnedAboveY(string higherResultType, string lowerResultType)
        {
            var apiResult = ScenarioContext.Current.Get<SearchAlgorithmApiResult>();
            apiResult.IsSuccess.Should().BeTrue();

            var results = apiResult.Results.SearchResults.ToList();

            var higherResult = ParseResultType(results, higherResultType);
            var lowerResult = ParseResultType(results, lowerResultType);

            results.Should().ContainInOrder(new List<SearchResult> {higherResult, lowerResult});
        }

        private static void AssertMatchGrade(
            TypePositions? expectedPosition,
            LocusSearchResult locusSearchResult,
            IReadOnlyCollection<MatchGrade> validMatchGrades
        )
        {
            if (expectedPosition == TypePositions.One || expectedPosition == TypePositions.Both)
            {
                validMatchGrades.Should().Contain(locusSearchResult.ScoreDetailsAtPositionOne.MatchGrade);
            }

            if (expectedPosition == TypePositions.Two || expectedPosition == TypePositions.Both)
            {
                validMatchGrades.Should().Contain(locusSearchResult.ScoreDetailsAtPositionTwo.MatchGrade);
            }
        }

        private static IEnumerable<MatchGrade> ParseExpectedMatchGrades(string grades)
        {
            switch (grades)
            {
                case "p-group":
                    return new[] {MatchGrade.PGroup};
                case "g-group":
                    return new[] {MatchGrade.GGroup};
                case "cDna":
                case "CDna":
                case "CDNA":
                case "cDNA":
                    return new[] {MatchGrade.CDna};
                case "gDna":
                case "GDna":
                case "gDNA":
                case "GDNA":
                    return new[] {MatchGrade.GDna};
                case "protein":
                    return new[] {MatchGrade.Protein};
                case "serology":
                    return new[] {MatchGrade.Associated, MatchGrade.Broad, MatchGrade.Split};
                default:
                    ScenarioContext.Current.Pending();
                    return new List<MatchGrade>();
            }
        }

        private static IEnumerable<Locus> ParseExpectedLoci(string locus)
        {
            var expectedLoci = new List<Locus>();

            switch (locus)
            {
                case "all loci":
                    expectedLoci.Add(Locus.A);
                    expectedLoci.Add(Locus.B);
                    expectedLoci.Add(Locus.C);
                    expectedLoci.Add(Locus.Dqb1);
                    expectedLoci.Add(Locus.Drb1);
                    break;
                default:
                    ScenarioContext.Current.Pending();
                    break;
            }

            return expectedLoci;
        }

        private static TypePositions? ParseExpectedPositions(string position)
        {
            switch (position)
            {
                case "both positions":
                    return TypePositions.Both;
                default:
                    ScenarioContext.Current.Pending();
                    return null;
            }
        }

        private static SearchResult ParseResultType(List<SearchResult> results, string resultType)
        {
            switch (resultType)
            {
                case "an 8/8 result":
                    return results.Find(r => r.TotalMatchCount == 8 && NumberOfLociSearched(r) == 4);
                case "a 7/8 result":
                    return results.Find(r => r.TotalMatchCount == 7 && NumberOfLociSearched(r) == 4);
                case "a 6/8 result":
                    return results.Find(r => r.TotalMatchCount == 6 && NumberOfLociSearched(r) == 4);
                case "a 5/8 result":
                    return results.Find(r => r.TotalMatchCount == 5 && NumberOfLociSearched(r) == 4);
                case "a 4/8 result":
                    return results.Find(r => r.TotalMatchCount == 4 && NumberOfLociSearched(r) == 4);
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
                default:
                    ScenarioContext.Current.Pending();
                    return null;
            }
        }

        private static bool IsMatchGradeAtMatchedLoci(SearchResult result, MatchGrade matchGrade)
        {
            return IsOneOfMatchGradesAtMatchedLoci(result, new[] {matchGrade});
        }

        private static bool IsOneOfMatchGradesAtMatchedLoci(SearchResult result, IEnumerable<MatchGrade> matchGrades)
        {
            var positionResults = new[]
            {
                result.SearchResultAtLocusA,
                result.SearchResultAtLocusA,
                result.SearchResultAtLocusB,
                result.SearchResultAtLocusB,
                result.SearchResultAtLocusC,
                result.SearchResultAtLocusC,
                result.SearchResultAtLocusDqb1,
                result.SearchResultAtLocusDqb1,
                result.SearchResultAtLocusDrb1,
                result.SearchResultAtLocusDrb1,
            };

            return positionResults.All(r =>
                // null match count implies not matched at that locus - we only want to assert grades of searched loci
                r.MatchCount == null
                || (matchGrades.Contains(r.ScoreDetailsAtPositionOne.MatchGrade) &&
                    matchGrades.Contains(r.ScoreDetailsAtPositionTwo.MatchGrade))
            );
        }

        private static int NumberOfLociSearched(SearchResult searchResult)
        {
            var loci = new[]
            {
                searchResult.SearchResultAtLocusA.MatchCount,
                searchResult.SearchResultAtLocusB.MatchCount,
                searchResult.SearchResultAtLocusC.MatchCount,
                searchResult.SearchResultAtLocusDqb1.MatchCount,
                searchResult.SearchResultAtLocusDrb1.MatchCount,
            };
            return loci.Count(x => x != null);
        }
    }
}