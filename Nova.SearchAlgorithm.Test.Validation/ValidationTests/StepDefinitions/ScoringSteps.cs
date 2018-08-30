using FluentAssertions;
using Nova.SearchAlgorithm.Client.Models.SearchResults;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Services.PatientDataSelection;
using System;
using System.Collections.Generic;
using System.Linq;
using TechTalk.SpecFlow;

namespace Nova.SearchAlgorithm.Test.Validation.ValidationTests.StepDefinitions
{
    [Binding]
    public class ScoringSteps
    {
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

        [Then(@"the match confidence should be (.*) at (.*) at (.*)")]
        public void ThenTheMatchConfidenceShouldBe(string confidence, string locus, string position)
        {
            var donorResult = GetSearchResultForSingleDonor();
            var validMatchConfidence = ParseExpectedMatchConfidence(confidence);
            var expectedLoci = ParseExpectedLoci(locus);
            var expectedPosition = ParseExpectedPositions(position);

            foreach (var expectedLocus in expectedLoci)
            {
                switch (expectedLocus)
                {
                    case Locus.A:
                        AssertMatchConfidence(expectedPosition, donorResult.SearchResultAtLocusA, validMatchConfidence);
                        break;
                    case Locus.B:
                        AssertMatchConfidence(expectedPosition, donorResult.SearchResultAtLocusB, validMatchConfidence);
                        break;
                    case Locus.C:
                        AssertMatchConfidence(expectedPosition, donorResult.SearchResultAtLocusC, validMatchConfidence);
                        break;
                    case Locus.Dpb1:
                        ScenarioContext.Current.Pending();
                        break;
                    case Locus.Dqb1:
                        AssertMatchConfidence(expectedPosition, donorResult.SearchResultAtLocusDqb1, validMatchConfidence);
                        break;
                    case Locus.Drb1:
                        AssertMatchConfidence(expectedPosition, donorResult.SearchResultAtLocusDrb1, validMatchConfidence);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private static SearchResult GetSearchResultForSingleDonor()
        {
            var patientDataSelector = ScenarioContext.Current.Get<IPatientDataFactory>();
            var apiResult = ScenarioContext.Current.Get<SearchAlgorithmApiResult>();
            apiResult.IsSuccess.Should().BeTrue();

            return apiResult
                .Results
                .SearchResults
                .Single(r => r.DonorId == patientDataSelector.GetExpectedMatchingDonorIds().Single());
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

        private static MatchConfidence? ParseExpectedMatchConfidence(string confidence)
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
                    ScenarioContext.Current.Pending();
                    return null;
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
                case "A":
                    expectedLoci.Add(Locus.A);
                    break;
                case "B":
                    expectedLoci.Add(Locus.B);
                    break;
                case "C":
                    expectedLoci.Add(Locus.C);
                    break;
                case "Dqb1":
                case "DQB1":
                    expectedLoci.Add(Locus.Dqb1);
                    break;
                case "Drb1":
                case "DRB1":
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

        private static void AssertMatchConfidence(
            TypePositions? expectedPosition,
            LocusSearchResult locusSearchResult,
            MatchConfidence? validMatchConfidence
        )
        {
            if (expectedPosition == TypePositions.One || expectedPosition == TypePositions.Both)
            {
                validMatchConfidence.Should().Be(locusSearchResult.ScoreDetailsAtPositionOne.MatchConfidence);
            }

            if (expectedPosition == TypePositions.Two || expectedPosition == TypePositions.Both)
            {
                validMatchConfidence.Should().Be(locusSearchResult.ScoreDetailsAtPositionTwo.MatchConfidence);
            }
        }
    }
}