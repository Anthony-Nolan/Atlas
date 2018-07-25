using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
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
    public class ConfidenceCalculatorTests
    {
        private ConfidenceCalculator confidenceCalculator;

        [SetUp]
        public void SetUp()
        {
            confidenceCalculator = new ConfidenceCalculator();
        }

        [Test]
        public void CalculateConfidence_BothTypingsMolecularAndSingleAllele_ReturnsDefininte()
        {
            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder().Build())
                .Build();

            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder().Build())
                .Build();

            var confidence = confidenceCalculator.CalculateConfidence(patientLookupResult, donorLookupResult);

            confidence.Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public void CalculateConfidence_BothTypingsMolecularAndSingleAllele_ButDoNotMatch_ReturnsMismatch()
        {
            const string donorPGroup = "p-group-1";
            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupResultCategory(LookupResultCategory.OriginalAllele)
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder().WithMatchingPGroup(donorPGroup).Build())
                .Build();

            const string patientPGroup = "p-group-2";
            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupResultCategory(LookupResultCategory.OriginalAllele)
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder().WithMatchingPGroup(patientPGroup).Build())
                .Build();

            var confidence = confidenceCalculator.CalculateConfidence(patientLookupResult, donorLookupResult);

            confidence.Should().Be(MatchConfidence.Mismatch);
        }

        // XX groups cannot map to a single p-group, so XxCodeScoringInfo does not need to be tested here
        [TestCase(typeof(SingleAlleleScoringInfo), typeof(MultipleAlleleScoringInfo))]
        [TestCase(typeof(MultipleAlleleScoringInfo), typeof(SingleAlleleScoringInfo))]
        [TestCase(typeof(MultipleAlleleScoringInfo), typeof(MultipleAlleleScoringInfo))]
        public void CalculateConfidence_BothTypingsMolecularAndSinglePGroup_ReturnsExact(Type donorScoringInfoType, Type patientScoringInfoType)
        {
            var patientLookupResult = BuildScoringLookupResultWithSinglePGroup(patientScoringInfoType);
            
            var donorLookupResult = BuildScoringLookupResultWithSinglePGroup(donorScoringInfoType);

            var confidence = confidenceCalculator.CalculateConfidence(patientLookupResult, donorLookupResult);

            confidence.Should().Be(MatchConfidence.Exact);
        }
        
        // XX groups cannot map to a single p-group, so XxCodeScoringInfo does not need to be tested here
        [TestCase(typeof(SingleAlleleScoringInfo), typeof(SingleAlleleScoringInfo))]
        [TestCase(typeof(SingleAlleleScoringInfo), typeof(MultipleAlleleScoringInfo))]
        [TestCase(typeof(MultipleAlleleScoringInfo), typeof(SingleAlleleScoringInfo))]
        [TestCase(typeof(MultipleAlleleScoringInfo), typeof(MultipleAlleleScoringInfo))]
        public void CalculateConfidence_BothTypingsMolecularAndSinglePGroup_ButDoNotMatch_ReturnsMismatch(Type donorScoringInfoType, Type patientScoringInfoType)
        {
            const string patientPGroup = "patient-p-group";
            var patientLookupResult = BuildScoringLookupResultWithSinglePGroup(patientScoringInfoType, patientPGroup);
            
            const string donorPGroup = "donor-p-group";
            var donorLookupResult = BuildScoringLookupResultWithSinglePGroup(donorScoringInfoType, donorPGroup);

            var confidence = confidenceCalculator.CalculateConfidence(patientLookupResult, donorLookupResult);

            confidence.Should().Be(MatchConfidence.Mismatch);
        }

        [TestCase(typeof(MultipleAlleleScoringInfo))]
        [TestCase(typeof(XxCodeScoringInfo))]
        public void CalculateConfidence_PatientSingleAllele_DonorMultiplePGroups_ReturnsPotential(Type donorScoringInfoType)
        {
            const string matchingPGroup = "p-group-match";
            
            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupResultCategory(LookupResultCategory.OriginalAllele)
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder().WithMatchingPGroup(matchingPGroup).Build())
                .Build();

            var donorPGroups = new List<string>{"donor-p-group-1", matchingPGroup};
            var donorLookupResult = BuildScoringLookupResultWithMultiplePGroups(donorScoringInfoType, donorPGroups);

            var confidence = confidenceCalculator.CalculateConfidence(patientLookupResult, donorLookupResult);

            confidence.Should().Be(MatchConfidence.Potential);
        }
        
        [TestCase(typeof(MultipleAlleleScoringInfo))]
        [TestCase(typeof(XxCodeScoringInfo))]
        public void CalculateConfidence_PatientMultiplePGroups_DonorSingleAllele_ReturnsPotential(Type patientScoringInfoType)
        {
            const string matchingPGroup = "p-group-match";
            
            var patientPGroups = new List<string>{"patient-p-group-1", matchingPGroup};
            var patientLookupResult = BuildScoringLookupResultWithMultiplePGroups(patientScoringInfoType, patientPGroups);
            
            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupResultCategory(LookupResultCategory.OriginalAllele)
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder().WithMatchingPGroup(matchingPGroup).Build())
                .Build();

            var confidence = confidenceCalculator.CalculateConfidence(patientLookupResult, donorLookupResult);

            confidence.Should().Be(MatchConfidence.Potential);
        }
        
        [TestCase(typeof(MultipleAlleleScoringInfo))]
        [TestCase(typeof(XxCodeScoringInfo))]
        public void CalculateConfidence_PatientSingleAllele_DonorMultiplePGroups_ButDoNotMatch_ReturnsMismatch(Type donorScoringInfoType)
        {
            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupResultCategory(LookupResultCategory.OriginalAllele)
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder().WithMatchingPGroup("patient-p-group").Build())
                .Build();

            var donorPGroups = new List<string>{"donor-p-group-1", "donor-p-group-2"};
            var donorLookupResult = BuildScoringLookupResultWithMultiplePGroups(donorScoringInfoType, donorPGroups);

            var confidence = confidenceCalculator.CalculateConfidence(patientLookupResult, donorLookupResult);

            confidence.Should().Be(MatchConfidence.Mismatch);
        }
        
        [TestCase(typeof(MultipleAlleleScoringInfo))]
        [TestCase(typeof(XxCodeScoringInfo))]
        public void CalculateConfidence_PatientMultiplePGroups_DonorSingleAllele_ButDoNotMatch_ReturnsMismatch(Type patientScoringInfoType)
        {
            var patientPGroups = new List<string>{"patient-p-group-1", "patient-p-group-2"};
            var patientLookupResult = BuildScoringLookupResultWithMultiplePGroups(patientScoringInfoType, patientPGroups);

            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupResultCategory(LookupResultCategory.OriginalAllele)
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder().WithMatchingPGroup("donor-p-group").Build())
                .Build();

            var confidence = confidenceCalculator.CalculateConfidence(patientLookupResult, donorLookupResult);

            confidence.Should().Be(MatchConfidence.Mismatch);
        }
        
        [TestCase(typeof(MultipleAlleleScoringInfo))]
        [TestCase(typeof(XxCodeScoringInfo))]
        public void CalculateConfidence_PatientSerology_DonorMultiplePGroups_ReturnsPotential(Type donorScoringInfoType)
        {
            var matchingSerologies = new List<SerologyEntry>{new SerologyEntry("serology", SerologySubtype.Associated, true)};

            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupResultCategory(LookupResultCategory.Serology)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder().WithMatchingSerologies(matchingSerologies).Build())
                .Build();

            var donorLookupResult = BuildScoringLookupResultWithMultiplePGroups(donorScoringInfoType, matchingSerologies: matchingSerologies);

            var confidence = confidenceCalculator.CalculateConfidence(patientLookupResult, donorLookupResult);

            confidence.Should().Be(MatchConfidence.Potential);
        }
        
        [TestCase(typeof(MultipleAlleleScoringInfo))]
        [TestCase(typeof(XxCodeScoringInfo))]
        public void CalculateConfidence_PatientMultiplePGroups_DonorSerology_ReturnsPotential(Type patientScoringInfoType)
        {
            var matchingSerologies = new List<SerologyEntry>{new SerologyEntry("serology", SerologySubtype.Associated, true)};

            var patientLookupResult = BuildScoringLookupResultWithMultiplePGroups(patientScoringInfoType, matchingSerologies: matchingSerologies);

            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupResultCategory(LookupResultCategory.Serology)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder().WithMatchingSerologies(matchingSerologies).Build())
                .Build();

            var confidence = confidenceCalculator.CalculateConfidence(patientLookupResult, donorLookupResult);

            confidence.Should().Be(MatchConfidence.Potential);
        }    
        
        [TestCase(typeof(MultipleAlleleScoringInfo))]
        [TestCase(typeof(XxCodeScoringInfo))]
        public void CalculateConfidence_PatientSerology_DonorMultiplePGroups_ButDoNotMatch_ReturnsMismatch(Type donorScoringInfoType)
        {
            var patientSerologyEntries = new List<SerologyEntry>{new SerologyEntry("serology-patient", SerologySubtype.Associated, true)};
            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupResultCategory(LookupResultCategory.Serology)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder().WithMatchingSerologies(patientSerologyEntries).Build())
                .Build();

            var donorSerologyEntries = new List<SerologyEntry>{new SerologyEntry("serology-donor", SerologySubtype.Associated, true)};
            var donorLookupResult = BuildScoringLookupResultWithMultiplePGroups(donorScoringInfoType, matchingSerologies: donorSerologyEntries);

            var confidence = confidenceCalculator.CalculateConfidence(patientLookupResult, donorLookupResult);

            confidence.Should().Be(MatchConfidence.Mismatch);
        }
        
        [TestCase(typeof(MultipleAlleleScoringInfo))]
        [TestCase(typeof(XxCodeScoringInfo))]
        public void CalculateConfidence_PatientMultiplePGroups_DonorSerology_ButDoNotMatch_ReturnsMismatch(Type patientScoringInfoType)
        {
            var patientSerologyEntries = new List<SerologyEntry>{new SerologyEntry("serology-patient", SerologySubtype.Associated, true)};
            var patientLookupResult = BuildScoringLookupResultWithMultiplePGroups(patientScoringInfoType, matchingSerologies: patientSerologyEntries);

            var donorSerologyEntries = new List<SerologyEntry>{new SerologyEntry("serology-donor", SerologySubtype.Associated, true)};
            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupResultCategory(LookupResultCategory.Serology)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder().WithMatchingSerologies(donorSerologyEntries).Build())
                .Build();

            var confidence = confidenceCalculator.CalculateConfidence(patientLookupResult, donorLookupResult);

            confidence.Should().Be(MatchConfidence.Mismatch);
        }
                
        [Test]
        public void CalculateConfidence_BothSerology_ReturnsPotential()
        {
            var matchingSerologies = new List<SerologyEntry>{new SerologyEntry("serology", SerologySubtype.Associated, true)};
            
            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupResultCategory(LookupResultCategory.Serology)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder().WithMatchingSerologies(matchingSerologies).Build())
                .Build();

            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupResultCategory(LookupResultCategory.Serology)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder().WithMatchingSerologies(matchingSerologies).Build())
                .Build();

            var confidence = confidenceCalculator.CalculateConfidence(patientLookupResult, donorLookupResult);

            confidence.Should().Be(MatchConfidence.Potential);
        }
        
        [Test]
        public void CalculateConfidence_BothSerology_ButDoNotMatch_ReturnsMismatch()
        {
            var patientSerologyEntries = new List<SerologyEntry>{new SerologyEntry("serology-patient", SerologySubtype.Associated, true)};
            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupResultCategory(LookupResultCategory.Serology)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder().WithMatchingSerologies(patientSerologyEntries).Build())
                .Build();

            var donorSerologyEntries = new List<SerologyEntry>{new SerologyEntry("serology-donor", SerologySubtype.Associated, true)};
            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupResultCategory(LookupResultCategory.Serology)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder().WithMatchingSerologies(donorSerologyEntries).Build())
                .Build();

            var confidence = confidenceCalculator.CalculateConfidence(patientLookupResult, donorLookupResult);

            confidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public void CalculateConfidence_PatientSerology_DonorSingleAllele_ReturnsPotential()
        {
            var matchingSerologies = new List<SerologyEntry>{new SerologyEntry("serology", SerologySubtype.Associated, true)};
            
            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupResultCategory(LookupResultCategory.Serology)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder().WithMatchingSerologies(matchingSerologies).Build())
                .Build();
            
            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupResultCategory(LookupResultCategory.OriginalAllele)
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder().WithMatchingSerologies(matchingSerologies).Build())
                .Build();

            var confidence = confidenceCalculator.CalculateConfidence(patientLookupResult, donorLookupResult);

            confidence.Should().Be(MatchConfidence.Potential);
        }
        
        [Test]
        public void CalculateConfidence_PatientSerology_DonorSingleAllele_ButDoNotMatch_ReturnsMismatch()
        {
            var patientSerologyEntries = new List<SerologyEntry>{new SerologyEntry("serology-patient", SerologySubtype.Associated, true)};
            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupResultCategory(LookupResultCategory.Serology)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder().WithMatchingSerologies(patientSerologyEntries).Build())
                .Build();
            
            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupResultCategory(LookupResultCategory.OriginalAllele)
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder().Build())
                .Build();

            var confidence = confidenceCalculator.CalculateConfidence(patientLookupResult, donorLookupResult);

            confidence.Should().Be(MatchConfidence.Mismatch);
        }
        
        [TestCase(typeof(MultipleAlleleScoringInfo), typeof(MultipleAlleleScoringInfo))]
        [TestCase(typeof(MultipleAlleleScoringInfo), typeof(XxCodeScoringInfo))]
        [TestCase(typeof(XxCodeScoringInfo), typeof(MultipleAlleleScoringInfo))]
        [TestCase(typeof(XxCodeScoringInfo), typeof(XxCodeScoringInfo))]
        public void CalculateConfidence_BothTypingsMolecularAndMultiplePGroups_ReturnsPotential(Type donorScoringInfoType, Type patientScoringInfoType)
        {
            var patientLookupResult = BuildScoringLookupResultWithMultiplePGroups(patientScoringInfoType);

            var donorLookupResult = BuildScoringLookupResultWithMultiplePGroups(donorScoringInfoType);

            var confidence = confidenceCalculator.CalculateConfidence(patientLookupResult, donorLookupResult);

            confidence.Should().Be(MatchConfidence.Potential);
        }
        
        [TestCase(typeof(MultipleAlleleScoringInfo), typeof(MultipleAlleleScoringInfo))]
        [TestCase(typeof(MultipleAlleleScoringInfo), typeof(XxCodeScoringInfo))]
        [TestCase(typeof(XxCodeScoringInfo), typeof(MultipleAlleleScoringInfo))]
        [TestCase(typeof(XxCodeScoringInfo), typeof(XxCodeScoringInfo))]
        public void CalculateConfidence_BothTypingsMolecularAndMultiplePGroups_ButDoNotMatch_ReturnsMismatch(Type donorScoringInfoType, Type patientScoringInfoType)
        {
            var patientLookupResult = BuildScoringLookupResultWithMultiplePGroups(patientScoringInfoType, new List<string>{"patient-p-group", "patient-p-group-2"});

            var donorLookupResult = BuildScoringLookupResultWithMultiplePGroups(donorScoringInfoType, new List<string>{"donor-p-group", "donor-p-group-2"});

            var confidence = confidenceCalculator.CalculateConfidence(patientLookupResult, donorLookupResult);

            confidence.Should().Be(MatchConfidence.Mismatch);
        }

        [Test]
        public void CalculateConfidence_DonorUntyped_ReturnsPotential()
        {
            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupResultCategory(LookupResultCategory.OriginalAllele)
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder().Build())
                .Build();

            var confidence = confidenceCalculator.CalculateConfidence(patientLookupResult, null);

            confidence.Should().Be(MatchConfidence.Potential);
        }
        
        [Test]
        public void CalculateConfidence_PatientUntyped_ReturnsPotential()
        {
            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithLookupResultCategory(LookupResultCategory.OriginalAllele)
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder().Build())
                .Build();

            var confidence = confidenceCalculator.CalculateConfidence(null, donorLookupResult);

            confidence.Should().Be(MatchConfidence.Potential);
        }

        private static HlaScoringLookupResult BuildScoringLookupResultWithSinglePGroup(Type scoringInfoType, string pGroupName = "single-p-group")
        {
            if (scoringInfoType == typeof(SingleAlleleScoringInfo))
            {
                return new HlaScoringLookupResultBuilder()
                    .WithLookupResultCategory(LookupResultCategory.OriginalAllele)
                    .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder().WithMatchingPGroup(pGroupName).Build())
                    .Build();
            }
            if (scoringInfoType == typeof(XxCodeScoringInfo))
            {
                return new HlaScoringLookupResultBuilder()
                    .WithLookupResultCategory(LookupResultCategory.XxCode)
                    .WithHlaScoringInfo(new XxCodeScoringInfoBuilder().WithMatchingPGroups(new List<string>{pGroupName}).Build())
                    .Build();
            }
            if (scoringInfoType == typeof(MultipleAlleleScoringInfo))
            {
                return new HlaScoringLookupResultBuilder()
                    .WithLookupResultCategory(LookupResultCategory.NmdpCodeAllele)
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

        private static HlaScoringLookupResult BuildScoringLookupResultWithMultiplePGroups(
            Type scoringInfoType, 
            IEnumerable<string> pGroupNames = null, 
            IEnumerable<SerologyEntry> matchingSerologies = null)
        {
            matchingSerologies = matchingSerologies ?? new List<SerologyEntry>();
            pGroupNames = pGroupNames ?? new List<string> {"p-group-1", "p-group-2"};
            if (scoringInfoType == typeof(XxCodeScoringInfo))
            {
                return new HlaScoringLookupResultBuilder()
                    .WithLookupResultCategory(LookupResultCategory.XxCode)
                    .WithHlaScoringInfo(new XxCodeScoringInfoBuilder()
                        .WithMatchingPGroups(pGroupNames)
                        .WithMatchingSerologies(matchingSerologies)
                        .Build()
                    )
                    .Build();
            }

            if (scoringInfoType == typeof(MultipleAlleleScoringInfo))
            {
                return new HlaScoringLookupResultBuilder()
                    .WithLookupResultCategory(LookupResultCategory.NmdpCodeAllele)
                    .WithHlaScoringInfo(new MultipleAlleleScoringInfoBuilder()
                        .WithAlleleScoringInfos(pGroupNames.Select(p =>
                            new SingleAlleleScoringInfoBuilder().WithMatchingPGroup(p).WithMatchingSerologies(matchingSerologies).Build()))
                        .Build()
                    )
                    .Build();
            }
            throw new Exception($"Unsupported type: {scoringInfoType}");
        }
    }
}