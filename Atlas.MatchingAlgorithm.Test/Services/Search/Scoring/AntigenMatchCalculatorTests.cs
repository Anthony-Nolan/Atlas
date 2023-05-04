using System;
using System.Collections.Generic;
using Atlas.Client.Models.Search.Results.Matching.PerLocus;
using Atlas.Common.Public.Models.GeneticData;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.HLATypings;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata.ScoringMetadata;
using Atlas.HlaMetadataDictionary.Test.TestHelpers.Builders;
using Atlas.HlaMetadataDictionary.Test.TestHelpers.Builders.ScoringInfoBuilders;
using Atlas.MatchingAlgorithm.Services.Search.Scoring.AntigenMatching;
using FluentAssertions;
using NUnit.Framework;

namespace Atlas.MatchingAlgorithm.Test.Services.Search.Scoring
{
    [TestFixture]
    internal class AntigenMatchCalculatorTests
    {
        private static readonly IHlaScoringMetadata DefaultScoringMetadata = new HlaScoringMetadataBuilder()
            .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder().Build())
            .Build();

        private IAntigenMatchCalculator antigenMatchCalculator;

        [SetUp]
        public void SetUp()
        {
            antigenMatchCalculator = new AntigenMatchCalculator();
        }

        [Test]
        public void IsAntigenMatch_PositionHasNotBeenGraded_ReturnsNull()
        {
            var result = antigenMatchCalculator.IsAntigenMatch(null, DefaultScoringMetadata, DefaultScoringMetadata);

            result.Should().BeNull();
        }

        [Test]
        public void IsAntigenMatch_MatchGradeIsUnknown_ReturnsNull()
        {
            var result = antigenMatchCalculator.IsAntigenMatch(MatchGrade.Unknown, DefaultScoringMetadata, DefaultScoringMetadata);

            result.Should().BeNull();
        }

        [Test]
        public void IsAntigenMatch_NoPatientMetadata_ReturnsNull()
        {
            var result = antigenMatchCalculator.IsAntigenMatch(MatchGrade.Mismatch, null, DefaultScoringMetadata);

            result.Should().BeNull();
        }

        [Test]
        public void IsAntigenMatch_NoDonorMetadata_ReturnsNull()
        {
            var result = antigenMatchCalculator.IsAntigenMatch(MatchGrade.Mismatch, DefaultScoringMetadata, null);

            result.Should().BeNull();
        }

        [Test]
        public void IsAntigenMatch_PatientAndDonorTypingsAreFromDifferentLoci_ThrowsException()
        {
            var patientMetadata = new HlaScoringMetadataBuilder().AtLocus(Locus.A).Build();
            var donorMetadata = new HlaScoringMetadataBuilder().AtLocus(Locus.B).Build();

            antigenMatchCalculator.Invoking(service => service.IsAntigenMatch(MatchGrade.Mismatch, patientMetadata, donorMetadata))
                .Should().Throw<Exception>();
        }

        [Test]
        public void IsAntigenMatch_NullVsNullAlleleMatchGrade_ReturnsFalse(
            [Values(
                MatchGrade.NullCDna, 
                MatchGrade.NullGDna, 
                MatchGrade.NullMismatch, 
                MatchGrade.NullPartial)] MatchGrade matchGrade)
        {
            var result = antigenMatchCalculator.IsAntigenMatch(matchGrade, DefaultScoringMetadata, DefaultScoringMetadata);

            result.Should().BeFalse();
        }

        [Test]
        public void IsAntigenMatch_NonMismatchExpressingMatchGrade_ReturnsTrue(
            [Values(            
                MatchGrade.GDna,
                MatchGrade.CDna,
                MatchGrade.Protein,
                MatchGrade.GGroup,
                MatchGrade.PGroup,
                MatchGrade.Associated,
                MatchGrade.Broad,
                MatchGrade.Split)] MatchGrade matchGrade)
        {
            var result = antigenMatchCalculator.IsAntigenMatch(matchGrade, DefaultScoringMetadata, DefaultScoringMetadata);

            result.Should().BeTrue();
        }

        [Test]
        public void IsAntigenMatch_MismatchMatchGrade_AndAntigenMatched_ReturnsTrue()
        {
            var matchingSerology = new SerologyEntry("matching-serology", SerologySubtype.Associated, true);

            var patientSerologies = new List<SerologyEntry>
            {
                matchingSerology,
                new("patient-only-serology", SerologySubtype.Associated, true)
            };
            var patientMetadata = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder().WithMatchingSerologies(patientSerologies).Build())
                .Build();

            var donorSerologies = new List<SerologyEntry>
            {
                matchingSerology,
                new("donor-only-serology", SerologySubtype.Associated, true)
            };
            var donorMetadata = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder().WithMatchingSerologies(donorSerologies).Build())
                .Build();

            var result = antigenMatchCalculator.IsAntigenMatch(MatchGrade.Mismatch, patientMetadata, donorMetadata);

            result.Should().BeTrue();
        }

        [Test]
        public void IsAntigenMatch_MismatchMatchGrade_AndAntigenMismatched_ReturnsFalse()
        {
            var patientSerologies = new List<SerologyEntry> { new("patient-only-serology", SerologySubtype.Associated, true) };
            var patientMetadata = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder().WithMatchingSerologies(patientSerologies).Build())
                .Build();

            var donorSerologies = new List<SerologyEntry> { new("donor-only-serology", SerologySubtype.Associated, true) };
            var donorMetadata = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder().WithMatchingSerologies(donorSerologies).Build())
                .Build();

            var result = antigenMatchCalculator.IsAntigenMatch(MatchGrade.Mismatch, patientMetadata, donorMetadata);

            result.Should().BeFalse();
        }
    }
}
