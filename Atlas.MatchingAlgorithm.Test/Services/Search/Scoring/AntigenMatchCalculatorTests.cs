using System;
using System.Collections.Generic;
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
        public void IsAntigenMatch_NoPatientMetadata_ReturnsNull()
        {
            var result = antigenMatchCalculator.IsAntigenMatch(null, DefaultScoringMetadata);

            result.Should().BeNull();
        }

        [Test]
        public void IsAntigenMatch_NoDonorMetadata_ReturnsNull()
        {
            var result = antigenMatchCalculator.IsAntigenMatch(DefaultScoringMetadata, null);

            result.Should().BeNull();
        }

        [Test]
        public void IsAntigenMatch_PatientAndDonorTypingsAreFromDifferentLoci_ThrowsException()
        {
            var patientMetadata = new HlaScoringMetadataBuilder().AtLocus(Locus.A).Build();
            var donorMetadata = new HlaScoringMetadataBuilder().AtLocus(Locus.B).Build();

            antigenMatchCalculator.Invoking(service => service.IsAntigenMatch(patientMetadata, donorMetadata))
                .Should().Throw<Exception>();
        }

        [Test]
        public void IsAntigenMatch_MolecularAndAlleleMatched_ReturnsTrue()
        {
            const string matchingPGroup = "matching-p-group";

            var patientScoringInfo = new SingleAlleleScoringInfoBuilder().WithMatchingPGroup(matchingPGroup).Build();
            var patientMetadata = new HlaScoringMetadataBuilder().WithHlaScoringInfo(patientScoringInfo).Build();

            var donorScoringInfo = new SingleAlleleScoringInfoBuilder().WithMatchingPGroup(matchingPGroup).Build();
            var donorMetadata = new HlaScoringMetadataBuilder().WithHlaScoringInfo(donorScoringInfo).Build();

            var result = antigenMatchCalculator.IsAntigenMatch(patientMetadata, donorMetadata);

            result.Should().BeTrue();
        }

        [Test]
        public void IsAntigenMatch_AlleleMismatched_AntigenMatched_ReturnsTrue()
        {
            var matchingSerology = new SerologyEntry("matching-serology", SerologySubtype.Associated, true);

            // assemble patient
            var patientSerologies = new List<SerologyEntry>
            {
                matchingSerology,
                new("patient-only-serology", SerologySubtype.Associated, true)
            };
            var patientScoringInfo = new SingleAlleleScoringInfoBuilder()
                .WithMatchingPGroup("patient-p-group")
                .WithMatchingSerologies(patientSerologies).Build();
            var patientMetadata = new HlaScoringMetadataBuilder().WithHlaScoringInfo(patientScoringInfo).Build();

            // assemble donor
            var donorSerologies = new List<SerologyEntry>
            {
                matchingSerology,
                new("donor-only-serology", SerologySubtype.Associated, true)
            };
            var donorScoringInfo = new SingleAlleleScoringInfoBuilder()
                .WithMatchingPGroup("donor-p-group")
                .WithMatchingSerologies(donorSerologies).Build();
            var donorMetadata = new HlaScoringMetadataBuilder().WithHlaScoringInfo(donorScoringInfo).Build();

            var result = antigenMatchCalculator.IsAntigenMatch(patientMetadata, donorMetadata);

            result.Should().BeTrue();
        }

        [Test]
        public void IsAntigenMatch_AlleleMismatched_AntigenMismatched_ReturnsFalse()
        {
            var patientSerologies = new List<SerologyEntry> { new("patient-only-serology", SerologySubtype.Associated, true) };
            var patientScoringInfo = new SingleAlleleScoringInfoBuilder()
                .WithMatchingPGroup("patient-p-group")
                .WithMatchingSerologies(patientSerologies).Build();
            var patientMetadata = new HlaScoringMetadataBuilder().WithHlaScoringInfo(patientScoringInfo).Build();

            var donorSerologies = new List<SerologyEntry> { new("donor-only-serology", SerologySubtype.Associated, true) };
            var donorScoringInfo = new SingleAlleleScoringInfoBuilder()
                .WithMatchingPGroup("donor-p-group")
                .WithMatchingSerologies(donorSerologies).Build();
            var donorMetadata = new HlaScoringMetadataBuilder().WithHlaScoringInfo(donorScoringInfo).Build();

            var result = antigenMatchCalculator.IsAntigenMatch(patientMetadata, donorMetadata);

            result.Should().BeFalse();
        }

        [Test]
        public void IsAntigenMatch_PatientIsNonExpressing_ReturnsFalse()
        {
            var patientMetadata = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new ConsolidatedMolecularScoringInfoBuilder().Build())
                .Build();

            var donorSerologies = new List<SerologyEntry> { new("donor-only-serology", SerologySubtype.Associated, true) };
            var donorMetadata = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder().WithMatchingSerologies(donorSerologies).Build())
                .Build();

            var result = antigenMatchCalculator.IsAntigenMatch(patientMetadata, donorMetadata);

            result.Should().BeFalse();
        }

        [Test]
        public void IsAntigenMatch_DonorIsNonExpressing_ReturnsFalse()
        {
            var patientSerologies = new List<SerologyEntry> { new("patient-only-serology", SerologySubtype.Associated, true) };
            var patientMetadata = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder().WithMatchingSerologies(patientSerologies).Build())
                .Build();

            var donorMetadata = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new ConsolidatedMolecularScoringInfoBuilder().Build())
                .Build();

            var result = antigenMatchCalculator.IsAntigenMatch(patientMetadata, donorMetadata);

            result.Should().BeFalse();
        }

        [Test]
        public void IsAntigenMatch_AlleleMismatched_PatientIsExpressingWithNoAssignedSerologies_ReturnsFalse()
        {
            var patientScoringInfo = new SingleAlleleScoringInfoBuilder().WithMatchingPGroup("patient-p-group").Build();
            var patientMetadata = new HlaScoringMetadataBuilder().WithHlaScoringInfo(patientScoringInfo).Build();

            var donorSerologies = new List<SerologyEntry> { new("donor-only-serology", SerologySubtype.Associated, true) };
            var donorScoringInfo = new SingleAlleleScoringInfoBuilder()
                .WithMatchingPGroup("donor-p-group")
                .WithMatchingSerologies(donorSerologies)
                .Build();
            var donorMetadata = new HlaScoringMetadataBuilder().WithHlaScoringInfo(donorScoringInfo).Build();

            var result = antigenMatchCalculator.IsAntigenMatch(patientMetadata, donorMetadata);

            result.Should().BeFalse();
        }

        [Test]
        public void IsAntigenMatch_AlleleMismatched_DonorIsExpressingWithNoAssignedSerologies_ReturnsFalse()
        {
            var patientSerologies = new List<SerologyEntry> { new("patient-only-serology", SerologySubtype.Associated, true) };
            var patientScoringInfo = new SingleAlleleScoringInfoBuilder()
                .WithMatchingPGroup("patient-p-group")
                .WithMatchingSerologies(patientSerologies)
                .Build();
            var patientMetadata = new HlaScoringMetadataBuilder().WithHlaScoringInfo(patientScoringInfo).Build();

            var donorScoringInfo = new SingleAlleleScoringInfoBuilder().WithMatchingPGroup("donor-p-group").Build();
            var donorMetadata = new HlaScoringMetadataBuilder().WithHlaScoringInfo(donorScoringInfo).Build();

            var result = antigenMatchCalculator.IsAntigenMatch(patientMetadata, donorMetadata);

            result.Should().BeFalse();
        }
    }
}
