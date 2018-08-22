using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Nova.SearchAlgorithm.Client.Models.SearchResults;
using Nova.SearchAlgorithm.Common.Models;
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
            var patientDataSelector = ScenarioContext.Current.Get<ISingleDonorPatientDataSelector>();
            var results = ScenarioContext.Current.Get<SearchResultSet>();
            var donorResult = results.SearchResults.Single(r => r.DonorId == patientDataSelector.GetExpectedMatchingDonorId());

            var matchGrade = ParseMatchGrade(grade);
            var expectedLoci = ParseExpectedLoci(locus);
            var expectedPosition = ParseExpectedPositions(position);

            foreach (var expectedLocus in expectedLoci)
            {
                switch (expectedLocus)
                {
                    case Locus.A:
                        AssertMatchGrade(expectedPosition, donorResult.SearchResultAtLocusA, matchGrade);
                        break;
                    case Locus.B:
                        AssertMatchGrade(expectedPosition, donorResult.SearchResultAtLocusB, matchGrade);
                        break;
                    case Locus.C:
                        AssertMatchGrade(expectedPosition, donorResult.SearchResultAtLocusC, matchGrade);
                        break;
                    case Locus.Dpb1:
                        ScenarioContext.Current.Pending();
                        break;
                    case Locus.Dqb1:
                        AssertMatchGrade(expectedPosition, donorResult.SearchResultAtLocusDqb1, matchGrade);
                        break;
                    case Locus.Drb1:
                        AssertMatchGrade(expectedPosition, donorResult.SearchResultAtLocusDrb1, matchGrade);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private static void AssertMatchGrade(TypePositions? expectedPosition, LocusSearchResult locusSearchResult, MatchGrade? matchGrade)
        {
            if (expectedPosition == TypePositions.One || expectedPosition == TypePositions.Both)
            {
                locusSearchResult.ScoreDetailsAtPositionOne.MatchGrade.Should().Be(matchGrade);
            }

            if (expectedPosition == TypePositions.Two || expectedPosition == TypePositions.Both)
            {
                locusSearchResult.ScoreDetailsAtPositionTwo.MatchGrade.Should().Be(matchGrade);
            }
        }

        private static MatchGrade? ParseMatchGrade(string grade)
        {
            switch (grade)
            {
                case "p-group":
                    return MatchGrade.PGroup;
                case "g-group":
                    return MatchGrade.GGroup;
                case "cDna":
                case "CDna":
                case "cDNA":
                    return MatchGrade.CDna;
                case "protein":
                    return MatchGrade.Protein;
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
    }
}