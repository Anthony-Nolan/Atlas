using System.Collections.Generic;
using FluentAssertions;
using Nova.SearchAlgorithm.Client.Models.SearchResults;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Common.Models.Scoring;
using Nova.SearchAlgorithm.MatchingDictionary.Models.HLATypings;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups;
using Nova.SearchAlgorithm.MatchingDictionary.Models.Lookups.ScoringLookup;
using Nova.SearchAlgorithm.Services.Scoring.Confidence;
using Nova.SearchAlgorithm.Test.Builders;
using Nova.SearchAlgorithm.Test.Builders.ScoringInfo;
using NUnit.Framework;

namespace Nova.SearchAlgorithm.Test.Services.Scoring.Confidence
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
            var confidenceCalculator = new ConfidenceCalculator();
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
        public void CalculateMatchConfidences_CalculatesMatchesForMultipleLoci()
        {
            const Locus locus1 = Locus.B;
            const Locus locus2 = Locus.Drb1;
            
            var patientLookupResults = new PhenotypeInfo<IHlaScoringLookupResult>();
            var patientLookupResultAtLocus1 = new HlaScoringLookupResultBuilder().Build();
            patientLookupResults.SetAtPosition(locus1, Position, patientLookupResultAtLocus1);
            patientLookupResults.SetAtPosition(locus2, Position, null);

            var donorLookupResults = new PhenotypeInfo<IHlaScoringLookupResult>();
            var donorLookupResultAtLocus1 = new HlaScoringLookupResultBuilder().Build();
            donorLookupResults.SetAtPosition(locus1, Position, donorLookupResultAtLocus1);
            donorLookupResults.SetAtPosition(locus2, Position, null);

            var gradingResults = defaultGradingResults;
            gradingResults.SetAtPosition(Locus, Position, new MatchGradeResult{ Orientations = new List<MatchOrientation>{ MatchOrientation.Direct }});

            var confidences = confidenceService.CalculateMatchConfidences(patientLookupResults, donorLookupResults, gradingResults);

            confidences.DataAtPosition(locus1, Position).Should().Be(MatchConfidence.Definite);
            confidences.DataAtPosition(locus2, Position).Should().Be(MatchConfidence.Potential);
        }

        [Test]
        public void CalculateMatchConfidences_WhenCrossOrientationProvided_ReturnsConfidenceForCrossPairsOfHlaData()
        {
            var matchingSerologies = new List<SerologyEntry>{new SerologyEntry("serology", SerologySubtype.Associated, true)};
            
            var patientLookupResults = new PhenotypeInfo<IHlaScoringLookupResult>();
            var patientLookupResultSingleAllele1 = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder().WithMatchingSerologies(matchingSerologies).Build())
                .Build();
            var patientLookupResultSingleAllele2 = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder().WithMatchingSerologies(matchingSerologies).Build())
                .Build();

            var donorLookupResults = new PhenotypeInfo<IHlaScoringLookupResult>();
            var donorLookupResultSerology = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder().WithMatchingSerologies(matchingSerologies).Build())
                .Build();
            var donorLookupResultSingleAllele = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder().WithMatchingSerologies(matchingSerologies).Build())
                .Build();
            
            
            patientLookupResults.SetAtPosition(Locus, TypePositions.One, patientLookupResultSingleAllele1);
            patientLookupResults.SetAtPosition(Locus, TypePositions.Two, patientLookupResultSingleAllele2);
            donorLookupResults.SetAtPosition(Locus, TypePositions.One, donorLookupResultSerology);
            donorLookupResults.SetAtPosition(Locus, TypePositions.Two, donorLookupResultSingleAllele);

            var gradingResults = defaultGradingResults;
            gradingResults.SetAtPosition(Locus, Position, new MatchGradeResult{ Orientations = new List<MatchOrientation>{ MatchOrientation.Cross }});
            
            var confidences = confidenceService.CalculateMatchConfidences(patientLookupResults, donorLookupResults, gradingResults);
            
            // Direct confidence (P1: D1) is Potential, Cross (P1: D2) is Definite
            confidences.DataAtPosition(Locus, TypePositions.One).Should().Be(MatchConfidence.Definite);
            // Direct confidence (P2: D2) is Definite, Cross (P2: D1) is Potential
            confidences.DataAtPosition(Locus, TypePositions.Two).Should().Be(MatchConfidence.Potential);
        }
        
        [Test]
        public void CalculateMatchConfidences_WhenDirectOrientationProvided_ReturnsConfidenceForDirectPairsOfHlaData()
        {
            var matchingSerologies = new List<SerologyEntry>{new SerologyEntry("serology", SerologySubtype.Associated, true)};
            
            var patientLookupResults = new PhenotypeInfo<IHlaScoringLookupResult>();
            var patientLookupResultSingleAllele1 = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder().WithMatchingSerologies(matchingSerologies).Build())
                .Build();
            var patientLookupResultSingleAllele2 = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder().WithMatchingSerologies(matchingSerologies).Build())
                .Build();

            var donorLookupResults = new PhenotypeInfo<IHlaScoringLookupResult>();
            var donorLookupResultSerology = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder().WithMatchingSerologies(matchingSerologies).Build())
                .Build();
            var donorLookupResultSingleAllele = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder().WithMatchingSerologies(matchingSerologies).Build())
                .Build();
            
            
            patientLookupResults.SetAtPosition(Locus, TypePositions.One, patientLookupResultSingleAllele1);
            patientLookupResults.SetAtPosition(Locus, TypePositions.Two, patientLookupResultSingleAllele2);
            donorLookupResults.SetAtPosition(Locus, TypePositions.One, donorLookupResultSerology);
            donorLookupResults.SetAtPosition(Locus, TypePositions.Two, donorLookupResultSingleAllele);

            var gradingResults = defaultGradingResults;
            gradingResults.SetAtPosition(Locus, Position, new MatchGradeResult{ Orientations = new List<MatchOrientation>{ MatchOrientation.Direct }});
            
            var confidences = confidenceService.CalculateMatchConfidences(patientLookupResults, donorLookupResults, gradingResults);
            
            // Direct confidence (P1: D1) is Potential, Cross (P1: D2) is Definite
            confidences.DataAtPosition(Locus, TypePositions.One).Should().Be(MatchConfidence.Potential);
            // Direct confidence (P2: D2) is Definite, Cross (P2: D1) is Potential
            confidences.DataAtPosition(Locus, TypePositions.Two).Should().Be(MatchConfidence.Definite);
        }
        
        [Test]
        public void CalculateMatchConfidences_WhenBothOrientationsProvided_AndBothOrientationsHaveSameEquivalentConfidence_ReturnsConfidenceForSingleOrientation()
        {
            var matchingSerologies = new List<SerologyEntry>{new SerologyEntry("serology", SerologySubtype.Associated, true)};
            
            var patientLookupResults = new PhenotypeInfo<IHlaScoringLookupResult>();
            var patientLookupResultSingleAllele1 = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder().WithMatchingSerologies(matchingSerologies).Build())
                .Build();
            var patientLookupResultSingleAllele2 = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder().WithMatchingSerologies(matchingSerologies).Build())
                .Build();

            var donorLookupResults = new PhenotypeInfo<IHlaScoringLookupResult>();
            var donorLookupResultSerology = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder().WithMatchingSerologies(matchingSerologies).Build())
                .Build();
            var donorLookupResultSingleAllele = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder().WithMatchingSerologies(matchingSerologies).Build())
                .Build();
            
            
            patientLookupResults.SetAtPosition(Locus, TypePositions.One, patientLookupResultSingleAllele1);
            patientLookupResults.SetAtPosition(Locus, TypePositions.Two, patientLookupResultSingleAllele2);
            donorLookupResults.SetAtPosition(Locus, TypePositions.One, donorLookupResultSerology);
            donorLookupResults.SetAtPosition(Locus, TypePositions.Two, donorLookupResultSingleAllele);

            var gradingResults = defaultGradingResults;
            gradingResults.SetAtPosition(Locus, Position, new MatchGradeResult{ Orientations = new List<MatchOrientation>{ MatchOrientation.Direct, MatchOrientation.Cross }});

            var confidences = confidenceService.CalculateMatchConfidences(patientLookupResults, donorLookupResults, gradingResults);

            var confidencesAtLocus = new List<MatchConfidence>
            {
                confidences.DataAtPosition(Locus, TypePositions.One),
                confidences.DataAtPosition(Locus, TypePositions.Two)
            };
            
            // Direct confidence (P1: D1) is Potential, Cross (P1: D2) is Definite
            // Direct confidence (P2: D2) is Definite, Cross (P2: D1) is Potential
            
            // As both confidence pairs are equivalent, it doesn't matter which position has which confidence, as long as they are not both the same
            confidencesAtLocus.Should().Contain(MatchConfidence.Definite);
            confidencesAtLocus.Should().Contain(MatchConfidence.Potential);
        }
        
        [Test]
        public void CalculateMatchConfidences_WhenBothOrientationsProvided_AndOneOrientationHasHigherConfidence_ReturnsConfidenceForBestOrientation()
        {
            var matchingSerologies = new List<SerologyEntry>{new SerologyEntry("serology", SerologySubtype.Associated, true)};
            
            var patientLookupResults = new PhenotypeInfo<IHlaScoringLookupResult>();
            var patientLookupResultSingleAllele = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder().WithMatchingSerologies(matchingSerologies).Build())
                .Build();
            var patientLookupResultSerology = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder().WithMatchingSerologies(matchingSerologies).Build())
                .Build();

            var donorLookupResults = new PhenotypeInfo<IHlaScoringLookupResult>();
            var donorLookupResultSerology = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder().WithMatchingSerologies(matchingSerologies).Build())
                .Build();
            var donorLookupResultSingleAllele = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder().WithMatchingSerologies(matchingSerologies).Build())
                .Build();
            
            patientLookupResults.SetAtPosition(Locus, TypePositions.One, patientLookupResultSingleAllele);
            patientLookupResults.SetAtPosition(Locus, TypePositions.Two, patientLookupResultSerology);
            donorLookupResults.SetAtPosition(Locus, TypePositions.One, donorLookupResultSerology);
            donorLookupResults.SetAtPosition(Locus, TypePositions.Two, donorLookupResultSingleAllele);

            var gradingResults = defaultGradingResults;
            gradingResults.SetAtPosition(Locus, Position, new MatchGradeResult{ Orientations = new List<MatchOrientation>{ MatchOrientation.Direct, MatchOrientation.Cross }});

            var confidences = confidenceService.CalculateMatchConfidences(patientLookupResults, donorLookupResults, gradingResults);
            
            // Direct confidence (P1: D1) is Potential, Cross (P1: D2) is Definite
            // Direct confidence (P2: D2) is Potential, Cross (P2: D1) is Potential
            
            // Cross match has a higher confidence, so should be returned
            confidences.DataAtPosition(Locus, TypePositions.One).Should().Be(MatchConfidence.Definite);
            confidences.DataAtPosition(Locus, TypePositions.Two).Should().Be(MatchConfidence.Potential);
        }
    }
}