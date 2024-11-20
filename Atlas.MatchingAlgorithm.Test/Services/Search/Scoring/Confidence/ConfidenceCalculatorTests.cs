using Atlas.HlaMetadataDictionary.ExternalInterface.Models.HLATypings;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata;
using Atlas.HlaMetadataDictionary.ExternalInterface.Models.Metadata.ScoringMetadata;
using Atlas.HlaMetadataDictionary.Test.TestHelpers.Builders;
using Atlas.HlaMetadataDictionary.Test.TestHelpers.Builders.ScoringInfoBuilders;
using Atlas.MatchingAlgorithm.Services.Search.Scoring.Confidence;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Atlas.Client.Models.Common.Results;

namespace Atlas.MatchingAlgorithm.Test.Services.Search.Scoring.Confidence
{
    [TestFixture]
    public class ConfidenceCalculatorTests
    {
        private ConfidenceCalculator confidenceCalculator;

        [SetUp]
        public void SetUp()
        {
            confidenceCalculator = new ConfidenceCalculator();
        }

        [Test]
        public void CalculateConfidence_BothTypingsMolecularAndSingleAllele_ReturnsDefinite()
        {
            var donorLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder().Build())
                .Build();

            var patientLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder().Build())
                .Build();

            var confidence = confidenceCalculator.CalculateConfidence(patientLookupResult, donorLookupResult);

            confidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public void CalculateConfidence_BothTypingsMolecularAndSingleAllele_ButDoNotMatch_ReturnsMismatch()
        {
            const string donorPGroup = "p-group-1";
            var donorLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder().WithMatchingPGroup(donorPGroup).Build())
                .Build();

            const string patientPGroup = "p-group-2";
            var patientLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder().WithMatchingPGroup(patientPGroup).Build())
                .Build();

            var confidence = confidenceCalculator.CalculateConfidence(patientLookupResult, donorLookupResult);

            confidence.Should().Be(MatchConfidence.Mismatch);
        }

        [TestCase(typeof(SingleAlleleScoringInfo), typeof(MultipleAlleleScoringInfo))]
        [TestCase(typeof(SingleAlleleScoringInfo), typeof(ConsolidatedMolecularScoringInfo))]
        [TestCase(typeof(MultipleAlleleScoringInfo), typeof(SingleAlleleScoringInfo))]
        [TestCase(typeof(MultipleAlleleScoringInfo), typeof(MultipleAlleleScoringInfo))]
        [TestCase(typeof(MultipleAlleleScoringInfo), typeof(ConsolidatedMolecularScoringInfo))]
        [TestCase(typeof(ConsolidatedMolecularScoringInfo), typeof(SingleAlleleScoringInfo))]
        [TestCase(typeof(ConsolidatedMolecularScoringInfo), typeof(MultipleAlleleScoringInfo))]
        [TestCase(typeof(ConsolidatedMolecularScoringInfo), typeof(ConsolidatedMolecularScoringInfo))]
        public void CalculateConfidence_BothTypingsMolecularAndSinglePGroup_ReturnsExact(Type donorScoringInfoType, Type patientScoringInfoType)
        {
            var patientLookupResult = BuildScoringMetadataWithSinglePGroup(patientScoringInfoType);
            
            var donorLookupResult = BuildScoringMetadataWithSinglePGroup(donorScoringInfoType);

            var confidence = confidenceCalculator.CalculateConfidence(patientLookupResult, donorLookupResult);

            confidence.Should().Be(MatchConfidence.Exact);
        }

        [TestCase(typeof(SingleAlleleScoringInfo), typeof(MultipleAlleleScoringInfo))]
        [TestCase(typeof(SingleAlleleScoringInfo), typeof(ConsolidatedMolecularScoringInfo))]
        [TestCase(typeof(MultipleAlleleScoringInfo), typeof(SingleAlleleScoringInfo))]
        [TestCase(typeof(MultipleAlleleScoringInfo), typeof(MultipleAlleleScoringInfo))]
        [TestCase(typeof(MultipleAlleleScoringInfo), typeof(ConsolidatedMolecularScoringInfo))]
        [TestCase(typeof(ConsolidatedMolecularScoringInfo), typeof(SingleAlleleScoringInfo))]
        [TestCase(typeof(ConsolidatedMolecularScoringInfo), typeof(MultipleAlleleScoringInfo))]
        [TestCase(typeof(ConsolidatedMolecularScoringInfo), typeof(ConsolidatedMolecularScoringInfo))]
        public void CalculateConfidence_BothTypingsMolecularAndSinglePGroup_ButDoNotMatch_ReturnsMismatch(Type donorScoringInfoType, Type patientScoringInfoType)
        {
            const string patientPGroup = "patient-p-group";
            var patientLookupResult = BuildScoringMetadataWithSinglePGroup(patientScoringInfoType, patientPGroup);
            
            const string donorPGroup = "donor-p-group";
            var donorLookupResult = BuildScoringMetadataWithSinglePGroup(donorScoringInfoType, donorPGroup);

            var confidence = confidenceCalculator.CalculateConfidence(patientLookupResult, donorLookupResult);

            confidence.Should().Be(MatchConfidence.Mismatch);
        }

        [TestCase(typeof(MultipleAlleleScoringInfo))]
        [TestCase(typeof(ConsolidatedMolecularScoringInfo))]
        public void CalculateConfidence_PatientSingleAllele_DonorMultiplePGroups_ReturnsPotential(Type donorScoringInfoType)
        {
            const string matchingPGroup = "p-group-match";
            
            var patientLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder().WithMatchingPGroup(matchingPGroup).Build())
                .Build();

            var donorPGroups = new List<string>{"donor-p-group-1", matchingPGroup};
            var donorLookupResult = BuildScoringMetadataWithMultiplePGroups(donorScoringInfoType, donorPGroups);

            var confidence = confidenceCalculator.CalculateConfidence(patientLookupResult, donorLookupResult);

            confidence.Should().Be(MatchConfidence.Potential);
        }
        
        [TestCase(typeof(MultipleAlleleScoringInfo))]
        [TestCase(typeof(ConsolidatedMolecularScoringInfo))]
        public void CalculateConfidence_PatientMultiplePGroups_DonorSingleAllele_ReturnsPotential(Type patientScoringInfoType)
        {
            const string matchingPGroup = "p-group-match";
            
            var patientPGroups = new List<string>{"patient-p-group-1", matchingPGroup};
            var patientLookupResult = BuildScoringMetadataWithMultiplePGroups(patientScoringInfoType, patientPGroups);
            
            var donorLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder().WithMatchingPGroup(matchingPGroup).Build())
                .Build();

            var confidence = confidenceCalculator.CalculateConfidence(patientLookupResult, donorLookupResult);

            confidence.Should().Be(MatchConfidence.Potential);
        }
        
        [TestCase(typeof(MultipleAlleleScoringInfo))]
        [TestCase(typeof(ConsolidatedMolecularScoringInfo))]
        public void CalculateConfidence_PatientSingleAllele_DonorMultiplePGroups_ButDoNotMatch_ReturnsMismatch(Type donorScoringInfoType)
        {
            var patientLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder().WithMatchingPGroup("patient-p-group").Build())
                .Build();

            var donorPGroups = new List<string>{"donor-p-group-1", "donor-p-group-2"};
            var donorLookupResult = BuildScoringMetadataWithMultiplePGroups(donorScoringInfoType, donorPGroups);

            var confidence = confidenceCalculator.CalculateConfidence(patientLookupResult, donorLookupResult);

            confidence.Should().Be(MatchConfidence.Mismatch);
        }
        
        [TestCase(typeof(MultipleAlleleScoringInfo))]
        [TestCase(typeof(ConsolidatedMolecularScoringInfo))]
        public void CalculateConfidence_PatientMultiplePGroups_DonorSingleAllele_ButDoNotMatch_ReturnsMismatch(Type patientScoringInfoType)
        {
            var patientPGroups = new List<string>{"patient-p-group-1", "patient-p-group-2"};
            var patientLookupResult = BuildScoringMetadataWithMultiplePGroups(patientScoringInfoType, patientPGroups);

            var donorLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder().WithMatchingPGroup("donor-p-group").Build())
                .Build();

            var confidence = confidenceCalculator.CalculateConfidence(patientLookupResult, donorLookupResult);

            confidence.Should().Be(MatchConfidence.Mismatch);
        }
        
        [TestCase(typeof(MultipleAlleleScoringInfo))]
        [TestCase(typeof(ConsolidatedMolecularScoringInfo))]
        public void CalculateConfidence_PatientSerology_DonorMultiplePGroups_ReturnsPotential(Type donorScoringInfoType)
        {
            var matchingSerologies = new List<SerologyEntry>{new SerologyEntry("serology", SerologySubtype.Associated, true)};

            var patientLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder().WithMatchingSerologies(matchingSerologies).Build())
                .Build();

            var donorLookupResult = BuildScoringMetadataWithMultiplePGroups(donorScoringInfoType, matchingSerologies: matchingSerologies);

            var confidence = confidenceCalculator.CalculateConfidence(patientLookupResult, donorLookupResult);

            confidence.Should().Be(MatchConfidence.Potential);
        }
        
        [TestCase(typeof(MultipleAlleleScoringInfo))]
        [TestCase(typeof(ConsolidatedMolecularScoringInfo))]
        public void CalculateConfidence_PatientMultiplePGroups_DonorSerology_ReturnsPotential(Type patientScoringInfoType)
        {
            var matchingSerologies = new List<SerologyEntry>{new SerologyEntry("serology", SerologySubtype.Associated, true)};

            var patientLookupResult = BuildScoringMetadataWithMultiplePGroups(patientScoringInfoType, matchingSerologies: matchingSerologies);

            var donorLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder().WithMatchingSerologies(matchingSerologies).Build())
                .Build();

            var confidence = confidenceCalculator.CalculateConfidence(patientLookupResult, donorLookupResult);

            confidence.Should().Be(MatchConfidence.Potential);
        }    
        
        [TestCase(typeof(MultipleAlleleScoringInfo))]
        [TestCase(typeof(ConsolidatedMolecularScoringInfo))]
        public void CalculateConfidence_PatientSerology_DonorMultiplePGroups_ButDoNotMatch_ReturnsMismatch(Type donorScoringInfoType)
        {
            var patientSerologyEntries = new List<SerologyEntry>{new SerologyEntry("serology-patient", SerologySubtype.Associated, true)};
            var patientLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder().WithMatchingSerologies(patientSerologyEntries).Build())
                .Build();

            var donorSerologyEntries = new List<SerologyEntry>{new SerologyEntry("serology-donor", SerologySubtype.Associated, true)};
            var donorLookupResult = BuildScoringMetadataWithMultiplePGroups(donorScoringInfoType, matchingSerologies: donorSerologyEntries);

            var confidence = confidenceCalculator.CalculateConfidence(patientLookupResult, donorLookupResult);

            confidence.Should().Be(MatchConfidence.Mismatch);
        }
        
        [TestCase(typeof(MultipleAlleleScoringInfo))]
        [TestCase(typeof(ConsolidatedMolecularScoringInfo))]
        public void CalculateConfidence_PatientMultiplePGroups_DonorSerology_ButDoNotMatch_ReturnsMismatch(Type patientScoringInfoType)
        {
            var patientSerologyEntries = new List<SerologyEntry>{new SerologyEntry("serology-patient", SerologySubtype.Associated, true)};
            var patientLookupResult = BuildScoringMetadataWithMultiplePGroups(patientScoringInfoType, matchingSerologies: patientSerologyEntries);

            var donorSerologyEntries = new List<SerologyEntry>{new SerologyEntry("serology-donor", SerologySubtype.Associated, true)};
            var donorLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder().WithMatchingSerologies(donorSerologyEntries).Build())
                .Build();

            var confidence = confidenceCalculator.CalculateConfidence(patientLookupResult, donorLookupResult);

            confidence.Should().Be(MatchConfidence.Mismatch);
        }
                
        [Test]
        public void CalculateConfidence_BothSerology_WithDirectMatch_ReturnsPotential()
        {
            var matchingSerologies = new List<SerologyEntry>{new SerologyEntry("serology", SerologySubtype.Associated, true)};
            
            var patientLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder().WithMatchingSerologies(matchingSerologies).Build())
                .Build();

            var donorLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder().WithMatchingSerologies(matchingSerologies).Build())
                .Build();

            var confidence = confidenceCalculator.CalculateConfidence(patientLookupResult, donorLookupResult);

            confidence.Should().Be(MatchConfidence.Potential);
        }

        [Test]
        public void CalculateConfidence_BothSerology_DonorIsIndirectlyMatchedToPatient_ReturnsPotential()
        {
            const string serologyName = "shared-serology";

            var patientSerologyEntries = new List<SerologyEntry> { new SerologyEntry(serologyName, SerologySubtype.Associated, true) };
            var patientLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder().WithMatchingSerologies(patientSerologyEntries).Build())
                .Build();

            var donorSerologyEntries = new List<SerologyEntry> { new SerologyEntry(serologyName, SerologySubtype.Associated, false) };
            var donorLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder().WithMatchingSerologies(donorSerologyEntries).Build())
                .Build();

            var confidence = confidenceCalculator.CalculateConfidence(patientLookupResult, donorLookupResult);

            confidence.Should().Be(MatchConfidence.Potential);
        }

        [Test]
        public void CalculateConfidence_BothSerology_PatientIsIndirectlyMatchedToDonor_ReturnsPotential()
        {
            const string serologyName = "shared-serology";

            var patientSerologyEntries = new List<SerologyEntry> { new SerologyEntry(serologyName, SerologySubtype.Associated, false) };
            var patientLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder().WithMatchingSerologies(patientSerologyEntries).Build())
                .Build();

            var donorSerologyEntries = new List<SerologyEntry> { new SerologyEntry(serologyName, SerologySubtype.Associated, true) };
            var donorLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder().WithMatchingSerologies(donorSerologyEntries).Build())
                .Build();

            var confidence = confidenceCalculator.CalculateConfidence(patientLookupResult, donorLookupResult);

            confidence.Should().Be(MatchConfidence.Potential);
        }

        [Test]
        public void CalculateConfidence_BothSerology_NoDirectOrIndirectMatch_ReturnsMismatch()
        {
            var patientSerologyEntries = new List<SerologyEntry>
            {
                new SerologyEntry("serology-patient-1", SerologySubtype.Associated, true),
                new SerologyEntry("serology-patient-2", SerologySubtype.Associated, false)
            };
            var patientLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder().WithMatchingSerologies(patientSerologyEntries).Build())
                .Build();

            var donorSerologyEntries = new List<SerologyEntry>
            {
                new SerologyEntry("serology-donor-1", SerologySubtype.Associated, true),
                new SerologyEntry("serology-donor-2", SerologySubtype.Associated, false)
            };
            var donorLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder().WithMatchingSerologies(donorSerologyEntries).Build())
                .Build();

            var confidence = confidenceCalculator.CalculateConfidence(patientLookupResult, donorLookupResult);

            confidence.Should().Be(MatchConfidence.Mismatch);
        }

        /// <summary>
        /// Regression test for live bug where sibling splits with shared broad were incorrectly assigned Potential instead of Mismatch.
        /// </summary>
        [Test]
        public void CalculateConfidence_BothSerology_NoDirectOrIndirectMatchButHaveSharedIndirectSerology_ReturnsMismatch()
        {
            const string sharedSerology = "shared-serology";

            var patientSerologyEntries = new List<SerologyEntry>
            {
                new SerologyEntry("patient-serology", SerologySubtype.Split, true),
                new SerologyEntry(sharedSerology, SerologySubtype.Broad, false)
            };
            var patientLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder().WithMatchingSerologies(patientSerologyEntries).Build())
                .Build();

            var donorSerologyEntries = new List<SerologyEntry>
            {
                new SerologyEntry("donor-serology", SerologySubtype.Split, true),
                new SerologyEntry(sharedSerology, SerologySubtype.Broad, false)
            };
            var donorLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder().WithMatchingSerologies(donorSerologyEntries).Build())
                .Build();

            var confidence = confidenceCalculator.CalculateConfidence(patientLookupResult, donorLookupResult);

            confidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public void CalculateConfidence_PatientSerology_DonorSingleAllele_ReturnsPotential()
        {
            var matchingSerologies = new List<SerologyEntry>{new SerologyEntry("serology", SerologySubtype.Associated, true)};
            
            var patientLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder().WithMatchingSerologies(matchingSerologies).Build())
                .Build();
            
            var donorLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder().WithMatchingSerologies(matchingSerologies).Build())
                .Build();

            var confidence = confidenceCalculator.CalculateConfidence(patientLookupResult, donorLookupResult);

            confidence.Should().Be(MatchConfidence.Potential);
        }
        
        [Test]
        public void CalculateConfidence_PatientSerology_DonorSingleAllele_ButDoNotMatch_ReturnsMismatch()
        {
            var patientSerologyEntries = new List<SerologyEntry>{new SerologyEntry("serology-patient", SerologySubtype.Associated, true)};
            var patientLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder().WithMatchingSerologies(patientSerologyEntries).Build())
                .Build();
            
            var donorLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder().Build())
                .Build();

            var confidence = confidenceCalculator.CalculateConfidence(patientLookupResult, donorLookupResult);

            confidence.Should().Be(MatchConfidence.Mismatch);
        }
        
        [TestCase(typeof(MultipleAlleleScoringInfo), typeof(MultipleAlleleScoringInfo))]
        [TestCase(typeof(MultipleAlleleScoringInfo), typeof(ConsolidatedMolecularScoringInfo))]
        [TestCase(typeof(ConsolidatedMolecularScoringInfo), typeof(MultipleAlleleScoringInfo))]
        [TestCase(typeof(ConsolidatedMolecularScoringInfo), typeof(ConsolidatedMolecularScoringInfo))]
        public void CalculateConfidence_BothTypingsMolecularAndMultiplePGroups_ReturnsPotential(Type donorScoringInfoType, Type patientScoringInfoType)
        {
            var patientLookupResult = BuildScoringMetadataWithMultiplePGroups(patientScoringInfoType);

            var donorLookupResult = BuildScoringMetadataWithMultiplePGroups(donorScoringInfoType);

            var confidence = confidenceCalculator.CalculateConfidence(patientLookupResult, donorLookupResult);

            confidence.Should().Be(MatchConfidence.Potential);
        }
        
        [TestCase(typeof(MultipleAlleleScoringInfo), typeof(MultipleAlleleScoringInfo))]
        [TestCase(typeof(MultipleAlleleScoringInfo), typeof(ConsolidatedMolecularScoringInfo))]
        [TestCase(typeof(ConsolidatedMolecularScoringInfo), typeof(MultipleAlleleScoringInfo))]
        [TestCase(typeof(ConsolidatedMolecularScoringInfo), typeof(ConsolidatedMolecularScoringInfo))]
        public void CalculateConfidence_BothTypingsMolecularAndMultiplePGroups_ButDoNotMatch_ReturnsMismatch(Type donorScoringInfoType, Type patientScoringInfoType)
        {
            var patientLookupResult = BuildScoringMetadataWithMultiplePGroups(patientScoringInfoType, new List<string>{"patient-p-group", "patient-p-group-2"});

            var donorLookupResult = BuildScoringMetadataWithMultiplePGroups(donorScoringInfoType, new List<string>{"donor-p-group", "donor-p-group-2"});

            var confidence = confidenceCalculator.CalculateConfidence(patientLookupResult, donorLookupResult);

            confidence.Should().Be(MatchConfidence.Mismatch);
        }

        #region Single Null Allele vs. Expressing

        [TestCase(typeof(SingleAlleleScoringInfo))]
        [TestCase(typeof(MultipleAlleleScoringInfo))]
        [TestCase(typeof(ConsolidatedMolecularScoringInfo))]
        public void CalculateConfidence_PatientHasSingleNullAllele_DonorHasExpressingMolecularTyping_ReturnsMismatch(Type donorScoringInfoType)
        {
            const string patientPGroup = null;
            var patientLookupResult = BuildScoringMetadataWithSinglePGroup(typeof(SingleAlleleScoringInfo), patientPGroup);

            const string donorPGroup = "p-group";
            var donorLookupResult = BuildScoringMetadataWithSinglePGroup(donorScoringInfoType, donorPGroup);

            var confidence = confidenceCalculator.CalculateConfidence(patientLookupResult, donorLookupResult);

            confidence.Should().Be(MatchConfidence.Mismatch);
        }

        [TestCase(typeof(SingleAlleleScoringInfo))]
        [TestCase(typeof(MultipleAlleleScoringInfo))]
        [TestCase(typeof(ConsolidatedMolecularScoringInfo))]
        public void CalculateConfidence_PatientHasExpressingMolecularTyping_DonorHasSingleNullAllele_ReturnsMismatch(Type patientScoringInfoType)
        {
            const string patientPGroup = "p-group";
            var patientLookupResult = BuildScoringMetadataWithSinglePGroup(patientScoringInfoType, patientPGroup);

            const string donorPGroup = null;
            var donorLookupResult = BuildScoringMetadataWithSinglePGroup(typeof(SingleAlleleScoringInfo), donorPGroup);

            var confidence = confidenceCalculator.CalculateConfidence(patientLookupResult, donorLookupResult);

            confidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public void CalculateConfidence_PatientHasSingleNullAllele_DonorHasSerology_ReturnsMismatch()
        {
            var patientSerologyEntries = new SerologyEntry[] { };
            var patientLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithMatchingPGroup(null)
                    .WithMatchingSerologies(patientSerologyEntries)
                    .Build())
                .Build();

            var donorSerologyEntries = new List<SerologyEntry> { new SerologyEntry("serology-donor", SerologySubtype.Associated, true) };
            var donorLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithMatchingSerologies(donorSerologyEntries)
                    .Build())
                .Build();

            var confidence = confidenceCalculator.CalculateConfidence(patientLookupResult, donorLookupResult);

            confidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public void CalculateConfidence_PatientHasSerology_DonorHasSingleNullAllele_ReturnsMismatch()
        {
            var patientSerologyEntries = new List<SerologyEntry> { new SerologyEntry("serology-patient", SerologySubtype.Associated, true) };
            var patientLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder()
                    .WithMatchingSerologies(patientSerologyEntries)
                    .Build())
                .Build();

            var donorSerologyEntries = new SerologyEntry[]{};
            var donorLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithMatchingPGroup(null)
                    .WithMatchingSerologies(donorSerologyEntries)
                    .Build())
                .Build();

            var confidence = confidenceCalculator.CalculateConfidence(patientLookupResult, donorLookupResult);

            confidence.Should().Be(MatchConfidence.Mismatch);
        }

        #endregion

        #region Single Null Allele vs. Single Null Allele

        [Test]
        public void CalculateConfidence_BothTypingsMolecular_AndSingleNullAllele_ReturnsDefinite()
        {
            var donorLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithMatchingPGroup(null)
                    .Build())
                .Build();

            var patientLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder()
                    .WithMatchingPGroup(null)
                    .Build())
                .Build();

            var confidence = confidenceCalculator.CalculateConfidence(patientLookupResult, donorLookupResult);

            confidence.Should().Be(MatchConfidence.Definite);
        }

        #endregion

        [Test]
        public void CalculateConfidence_DonorUntyped_ReturnsPotential()
        {
            var patientLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder().Build())
                .Build();

            var confidence = confidenceCalculator.CalculateConfidence(patientLookupResult, null);

            confidence.Should().Be(MatchConfidence.Potential);
        }
        
        [Test]
        public void CalculateConfidence_PatientUntyped_ReturnsPotential()
        {
            var donorLookupResult = new HlaScoringMetadataBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder().Build())
                .Build();

            var confidence = confidenceCalculator.CalculateConfidence(null, donorLookupResult);

            confidence.Should().Be(MatchConfidence.Potential);
        }

        private static HlaScoringMetadata BuildScoringMetadataWithSinglePGroup(Type scoringInfoType, string pGroupName = "single-p-group")
        {
            if (scoringInfoType == typeof(SingleAlleleScoringInfo))
            {
                return new HlaScoringMetadataBuilder()
                    .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder().WithMatchingPGroup(pGroupName).Build())
                    .Build();
            }
            if (scoringInfoType == typeof(ConsolidatedMolecularScoringInfo))
            {
                return new HlaScoringMetadataBuilder()
                    .WithHlaScoringInfo(new ConsolidatedMolecularScoringInfoBuilder().WithMatchingPGroups(new List<string>{pGroupName}).Build())
                    .Build();
            }
            if (scoringInfoType == typeof(MultipleAlleleScoringInfo))
            {
                return new HlaScoringMetadataBuilder()
                    .WithHlaScoringInfo(new MultipleAlleleScoringInfoBuilder()
                        .WithAlleleScoringInfos(new List<SingleAlleleScoringInfo>
                        {
                            new SingleAlleleScoringInfoBuilder().WithMatchingPGroup(pGroupName).Build()
                        })
                        .Build()
                    )
                    .Build();
            }
            throw new Exception($"Unsupported type: {scoringInfoType}");
        }

        private static HlaScoringMetadata BuildScoringMetadataWithMultiplePGroups(
            Type scoringInfoType, 
            IEnumerable<string> pGroupNames = null, 
            IEnumerable<SerologyEntry> matchingSerologies = null)
        {
            matchingSerologies = matchingSerologies ?? new List<SerologyEntry>();
            pGroupNames = pGroupNames ?? new List<string> {"p-group-1", "p-group-2"};
            if (scoringInfoType == typeof(ConsolidatedMolecularScoringInfo))
            {
                return new HlaScoringMetadataBuilder()
                    .WithHlaScoringInfo(new ConsolidatedMolecularScoringInfoBuilder()
                        .WithMatchingPGroups(pGroupNames)
                        .WithMatchingSerologies(matchingSerologies)
                        .Build()
                    )
                    .Build();
            }

            if (scoringInfoType == typeof(MultipleAlleleScoringInfo))
            {
                return new HlaScoringMetadataBuilder()
                    .WithHlaScoringInfo(new MultipleAlleleScoringInfoBuilder()
                        .WithAlleleScoringInfos(pGroupNames.Select(p =>
                            new SingleAlleleScoringInfoBuilder().WithMatchingPGroup(p).Build()))
                        .WithMatchingSerologies(matchingSerologies)
                        .Build())
                    .Build();
            }
            throw new Exception($"Unsupported type: {scoringInfoType}");
        }
    }
}