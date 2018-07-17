using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Nova.HLAService.Client.Models;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Models.Scoring;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.ScoringLookup;
using Nova.SearchAlgorithm.Services.Scoring;
using Nova.SearchAlgorithm.Services.Scoring.Confidence;
using Nova.SearchAlgorithm.Test.Builders;
using Nova.SearchAlgorithm.Test.Builders.ScoringInfo;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.Services.Scoring
{
    [TestFixture]
    public class ConfidenceServiceTests
    {
        // Unless specified otherwise, all tests will be at a shared locus + position, to reduce setup in the individual test cases
        private const Locus Locus = Common.Models.Locus.A;
        private const TypePositions Position = TypePositions.One;
        
        private IConfidenceService confidenceService;
        private readonly MatchGradeResult defaultGradingResult = new MatchGradeResult{Orientations = new List<MatchOrientation>{ MatchOrientation.Direct }};
        private PhenotypeInfo<MatchGradeResult> defaultGradingResults;

        [SetUp]
        public void SetUp()
        {
            var confidenceCalculator = new ConfidenceCalculator();;
            confidenceService = new ConfidenceService(confidenceCalculator);
            defaultGradingResults = new PhenotypeInfo<MatchGradeResult>
            {
                A_1 = defaultGradingResult,
                A_2 = defaultGradingResult,
                B_1 = defaultGradingResult,
                B_2 = defaultGradingResult,
                C_1 = defaultGradingResult,
                C_2 = defaultGradingResult,
                DPB1_1 = defaultGradingResult,
                DPB1_2 = defaultGradingResult,
                DQB1_1 = defaultGradingResult,
                DQB1_2 = defaultGradingResult,
                DRB1_1 = defaultGradingResult,
                DRB1_2 = defaultGradingResult,
            };
        }

        [Test]
        public void CalculateMatchConfidences_ReturnsDifferentResultsForDifferentLoci()
        {
            const Locus locus1 = Locus.B;
            const Locus locus2 = Locus.Drb1;
            
            var patientLookupResults = new PhenotypeInfo<IHlaScoringLookupResult>();
            var patientLookupResultAtLocus1 = new HlaScoringLookupResultBuilder().WithHlaTypingCategory(HlaTypingCategory.Allele).Build();
            patientLookupResults.SetAtLocus(locus1, Position, patientLookupResultAtLocus1);
            patientLookupResults.SetAtLocus(locus2, Position, null);

            var donorLookupResults = new PhenotypeInfo<IHlaScoringLookupResult>();
            var donorLookupResultAtLocus1 = new HlaScoringLookupResultBuilder().WithHlaTypingCategory(HlaTypingCategory.Allele).Build();
            donorLookupResults.SetAtLocus(locus1, Position, donorLookupResultAtLocus1);
            donorLookupResults.SetAtLocus(locus2, Position, null);

            var gradingResults = defaultGradingResults;
            gradingResults.SetAtLocus(Locus, Position, new MatchGradeResult{ Orientations = new List<MatchOrientation>{ MatchOrientation.Direct }});

            var confidences = confidenceService.CalculateMatchConfidences(patientLookupResults, donorLookupResults, gradingResults);

            confidences.DataAtPosition(locus1, Position).Should().NotBe(confidences.DataAtPosition(locus2, Position));
        }
        
        [Test]
        public void CalculateMatchConfidences_WhenMultipleOrientationsProvided_ReturnsBestConfidenceAmongstOrientations()
        {
            const string matchingPGroup = "matching-p-group";
            var matchingSerologies = new List<SerologyEntry>{new SerologyEntry("serology", SerologySubtype.Associated)};
            
            var patientLookupResults = new PhenotypeInfo<IHlaScoringLookupResult>();
            var patientLookupResultAtPosition1 = new HlaScoringLookupResultBuilder()
                .WithHlaTypingCategory(HlaTypingCategory.Allele)
                .WithHlaScoringInfo(
                    new SingleAlleleScoringInfoBuilder()
                        .WithMatchingPGroup(matchingPGroup)
                        .WithMatchingSerologies(matchingSerologies)
                        .Build())
                .Build();
            patientLookupResults.SetAtLocus(Locus, TypePositions.One, patientLookupResultAtPosition1);

            var donorLookupResults = new PhenotypeInfo<IHlaScoringLookupResult>();
            var donorLookupResultAtPosition1 = new HlaScoringLookupResultBuilder()
                .WithHlaTypingCategory(HlaTypingCategory.Serology)
                .WithHlaScoringInfo(
                    new SerologyScoringInfoBuilder()
                        .WithMatchingSerologies(matchingSerologies)
                        .Build())
                .Build();
            
            var donorLookupResultAtPosition2 = new HlaScoringLookupResultBuilder()
                .WithHlaTypingCategory(HlaTypingCategory.Allele)
                .WithHlaScoringInfo(
                    new SingleAlleleScoringInfoBuilder()
                        .WithMatchingPGroup(matchingPGroup)
                        .Build())
                .Build();
            donorLookupResults.SetAtLocus(Locus, TypePositions.One, donorLookupResultAtPosition1);
            donorLookupResults.SetAtLocus(Locus, TypePositions.Two, donorLookupResultAtPosition2);

            var gradingResults = defaultGradingResults;
            gradingResults.SetAtLocus(Locus, Position, new MatchGradeResult{ Orientations = new List<MatchOrientation>{ MatchOrientation.Direct, MatchOrientation.Cross }});

            // Cross confidence is definite, direct is potential 
            var confidences = confidenceService.CalculateMatchConfidences(patientLookupResults, donorLookupResults, gradingResults);

            confidences.DataAtPosition(Locus, Position).Should().Be(MatchConfidence.Definite);
        }
        
        [Test]
        public void CalculateMatchConfidences_WhenOneOrientationProvided_ReturnsConfidenceForSpecifiedOrientation()
        {
            var matchingSerologies = new List<SerologyEntry>{new SerologyEntry("serology", SerologySubtype.Associated)};
            
            var patientLookupResults = new PhenotypeInfo<IHlaScoringLookupResult>();
            var patientLookupResultAtPosition1 = new HlaScoringLookupResultBuilder()
                .WithHlaTypingCategory(HlaTypingCategory.Allele)
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder().WithMatchingSerologies(matchingSerologies).Build())
                .Build();
            patientLookupResults.SetAtLocus(Locus, TypePositions.One, patientLookupResultAtPosition1);

            var donorLookupResults = new PhenotypeInfo<IHlaScoringLookupResult>();
            var donorLookupResultAtPosition1 = new HlaScoringLookupResultBuilder()
                .WithHlaTypingCategory(HlaTypingCategory.Serology)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder().WithMatchingSerologies(matchingSerologies).Build())
                .Build();
            var donorLookupResultAtPosition2 = new HlaScoringLookupResultBuilder().WithHlaTypingCategory(HlaTypingCategory.Allele).Build();
            donorLookupResults.SetAtLocus(Locus, TypePositions.One, donorLookupResultAtPosition1);
            donorLookupResults.SetAtLocus(Locus, TypePositions.Two, donorLookupResultAtPosition2);

            var gradingResults = defaultGradingResults;
            gradingResults.SetAtLocus(Locus, Position, new MatchGradeResult{ Orientations = new List<MatchOrientation>{ MatchOrientation.Direct }});

            // Cross confidence is definite, direct is potential 
            var confidences = confidenceService.CalculateMatchConfidences(patientLookupResults, donorLookupResults, gradingResults);

            confidences.DataAtPosition(Locus, TypePositions.One).Should().Be(MatchConfidence.Potential);
        }
    }
}