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

        [SetUp]
        public void SetUp()
        {
            confidenceService = new ConfidenceService();
        }

        [Test]
        public void CalculateMatchConfidences_BothTypingsMolecularAndSingleAllele_ReturnsDefininte()
        {
            var donorLookupResults = new PhenotypeInfo<IHlaScoringLookupResult>();
            var donorLookupResult = new HlaScoringLookupResultBuilder().WithHlaTypingCategory(HlaTypingCategory.Allele).Build();
            donorLookupResults.SetAtLocus(Locus, Position, donorLookupResult);

            var patientLookupResults = new PhenotypeInfo<IHlaScoringLookupResult>();
            var patientLookupResult = new HlaScoringLookupResultBuilder().WithHlaTypingCategory(HlaTypingCategory.Allele).Build();
            patientLookupResults.SetAtLocus(Locus, Position, patientLookupResult);

            var gradingResults = new PhenotypeInfo<MatchGradeResult>();

            var confidences = confidenceService.CalculateMatchConfidences(donorLookupResults, patientLookupResults, gradingResults);

            confidences.DataAtPosition(Locus, Position).Should().Be(MatchConfidence.Definite);
        }

        [Test]
        public void CalculateMatchConfidences_BothTypingsMolecularAndSingleAllele_ButDoNotMatch_ReturnsMismatch()
        {
            var donorLookupResults = new PhenotypeInfo<IHlaScoringLookupResult>();
            const string donorPGroup = "p-group-1";
            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaTypingCategory(HlaTypingCategory.Allele)
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder().WithMatchingPGroup(donorPGroup).Build())
                .Build();
            donorLookupResults.SetAtLocus(Locus, Position, donorLookupResult);

            var patientLookupResults = new PhenotypeInfo<IHlaScoringLookupResult>();
            const string patientPGroup = "p-group-2";
            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaTypingCategory(HlaTypingCategory.Allele)
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder().WithMatchingPGroup(patientPGroup).Build())
                .Build();
            patientLookupResults.SetAtLocus(Locus, Position, patientLookupResult);

            var gradingResults = new PhenotypeInfo<MatchGradeResult>();

            var confidences = confidenceService.CalculateMatchConfidences(donorLookupResults, patientLookupResults, gradingResults);

            confidences.DataAtPosition(Locus, Position).Should().Be(MatchConfidence.Definite);
        }

        [TestCase(typeof(SingleAlleleScoringInfo), typeof(SingleAlleleScoringInfo))]
        [TestCase(typeof(SingleAlleleScoringInfo), typeof(AlleleStringScoringInfo))]
        [TestCase(typeof(SingleAlleleScoringInfo), typeof(XxCodeScoringInfo))]
        [TestCase(typeof(AlleleStringScoringInfo), typeof(SingleAlleleScoringInfo))]
        [TestCase(typeof(AlleleStringScoringInfo), typeof(AlleleStringScoringInfo))]
        [TestCase(typeof(AlleleStringScoringInfo), typeof(XxCodeScoringInfo))]
        [TestCase(typeof(XxCodeScoringInfo), typeof(SingleAlleleScoringInfo))]
        [TestCase(typeof(XxCodeScoringInfo), typeof(AlleleStringScoringInfo))]
        [TestCase(typeof(XxCodeScoringInfo), typeof(XxCodeScoringInfo))]
        public void CalculateMatchConfidences_BothTypingsMolecularAndSinglePGroup_ReturnsExact(Type donorScoringInfoType, Type patientScoringInfoType)
        {
            var donorLookupResults = new PhenotypeInfo<IHlaScoringLookupResult>();
            var donorLookupResult = BuildScoringLookupResultWithSinglePGroup(donorScoringInfoType);
            donorLookupResults.SetAtLocus(Locus, Position, donorLookupResult);

            var patientLookupResults = new PhenotypeInfo<IHlaScoringLookupResult>();            
            var patientLookupResult = BuildScoringLookupResultWithSinglePGroup(patientScoringInfoType);
            patientLookupResults.SetAtLocus(Locus, Position, patientLookupResult);

            var gradingResults = new PhenotypeInfo<MatchGradeResult>();

            var confidences = confidenceService.CalculateMatchConfidences(donorLookupResults, patientLookupResults, gradingResults);

            confidences.DataAtPosition(Locus, Position).Should().Be(MatchConfidence.Exact);
        }
        
        [TestCase(typeof(SingleAlleleScoringInfo), typeof(SingleAlleleScoringInfo))]
        [TestCase(typeof(SingleAlleleScoringInfo), typeof(AlleleStringScoringInfo))]
        [TestCase(typeof(SingleAlleleScoringInfo), typeof(XxCodeScoringInfo))]
        [TestCase(typeof(AlleleStringScoringInfo), typeof(SingleAlleleScoringInfo))]
        [TestCase(typeof(AlleleStringScoringInfo), typeof(AlleleStringScoringInfo))]
        [TestCase(typeof(AlleleStringScoringInfo), typeof(XxCodeScoringInfo))]
        [TestCase(typeof(XxCodeScoringInfo), typeof(SingleAlleleScoringInfo))]
        [TestCase(typeof(XxCodeScoringInfo), typeof(AlleleStringScoringInfo))]
        [TestCase(typeof(XxCodeScoringInfo), typeof(XxCodeScoringInfo))]
        public void CalculateMatchConfidences_BothTypingsMolecularAndSinglePGroup_ButDoNotMatch_ReturnsMismatch(Type donorScoringInfoType, Type patientScoringInfoType)
        {
            var donorLookupResults = new PhenotypeInfo<IHlaScoringLookupResult>();
            const string donorPGroup = "donor-p-group";
            var donorLookupResult = BuildScoringLookupResultWithSinglePGroup(donorScoringInfoType, donorPGroup);
            donorLookupResults.SetAtLocus(Locus, Position, donorLookupResult);

            var patientLookupResults = new PhenotypeInfo<IHlaScoringLookupResult>();
            const string patientPGroup = "donor-p-group";
            var patientLookupResult = BuildScoringLookupResultWithSinglePGroup(patientScoringInfoType, patientPGroup);
            patientLookupResults.SetAtLocus(Locus, Position, patientLookupResult);

            var gradingResults = new PhenotypeInfo<MatchGradeResult>();

            var confidences = confidenceService.CalculateMatchConfidences(donorLookupResults, patientLookupResults, gradingResults);

            confidences.DataAtPosition(Locus, Position).Should().Be(MatchConfidence.Mismatch);
        }

        [TestCase(typeof(AlleleStringScoringInfo))]
        [TestCase(typeof(XxCodeScoringInfo))]
        public void CalculateMatchConfidences_PatientSingleAllele_DonorMultiplePGroups_ReturnsPotential(Type donorScoringInfoType)
        {
            const string matchingPGroup = "p-group-match";
            
            var donorLookupResults = new PhenotypeInfo<IHlaScoringLookupResult>();
            var donorPGroups = new List<string>{"donor-p-group-1", matchingPGroup};
            var donorLookupResult = BuildScoringLookupResultWithMultiplePGroups(donorScoringInfoType, donorPGroups);
            donorLookupResults.SetAtLocus(Locus, Position, donorLookupResult);

            var patientLookupResults = new PhenotypeInfo<IHlaScoringLookupResult>();
            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaTypingCategory(HlaTypingCategory.Allele)
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder().WithMatchingPGroup(matchingPGroup).Build())
                .Build();
            patientLookupResults.SetAtLocus(Locus, Position, patientLookupResult);

            var gradingResults = new PhenotypeInfo<MatchGradeResult>();

            var confidences = confidenceService.CalculateMatchConfidences(donorLookupResults, patientLookupResults, gradingResults);

            confidences.DataAtPosition(Locus, Position).Should().Be(MatchConfidence.Potential);
        }
        
        [TestCase(typeof(AlleleStringScoringInfo))]
        [TestCase(typeof(XxCodeScoringInfo))]
        public void CalculateMatchConfidences_PatientMultiplePGroups_DonorSingleAllele_ReturnsPotential(Type patientScoringInfoType)
        {
            const string matchingPGroup = "p-group-match";
            
            var donorLookupResults = new PhenotypeInfo<IHlaScoringLookupResult>();
            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaTypingCategory(HlaTypingCategory.Allele)
                .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder().WithMatchingPGroup(matchingPGroup).Build())
                .Build();
            donorLookupResults.SetAtLocus(Locus, Position, donorLookupResult);
            
            var patientLookupResults = new PhenotypeInfo<IHlaScoringLookupResult>();
            var patientPGroups = new List<string>{"patient-p-group-1", matchingPGroup};
            var patientLookupResult = BuildScoringLookupResultWithMultiplePGroups(patientScoringInfoType, patientPGroups);
            patientLookupResults.SetAtLocus(Locus, Position, patientLookupResult);

            var gradingResults = new PhenotypeInfo<MatchGradeResult>();

            var confidences = confidenceService.CalculateMatchConfidences(donorLookupResults, patientLookupResults, gradingResults);

            confidences.DataAtPosition(Locus, Position).Should().Be(MatchConfidence.Potential);
        }
        
        [TestCase(typeof(AlleleStringScoringInfo))]
        [TestCase(typeof(XxCodeScoringInfo))]
        public void CalculateMatchConfidences_PatientSerology_DonorMultiplePGroups_ReturnsPotential(Type donorScoringInfoType)
        {
            var serologyEntries = new List<SerologyEntry>{new SerologyEntry("serology", SerologySubtype.Associated)};

            var donorLookupResults = new PhenotypeInfo<IHlaScoringLookupResult>();
            var donorLookupResult = BuildScoringLookupResultWithMultiplePGroups(donorScoringInfoType, serologyEntries: serologyEntries);
            donorLookupResults.SetAtLocus(Locus, Position, donorLookupResult);

            var patientLookupResults = new PhenotypeInfo<IHlaScoringLookupResult>();
            var patientLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaTypingCategory(HlaTypingCategory.Serology)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder().WithMatchingSerologies(serologyEntries).Build())
                .Build();
            patientLookupResults.SetAtLocus(Locus, Position, patientLookupResult);

            var gradingResults = new PhenotypeInfo<MatchGradeResult>();

            var confidences = confidenceService.CalculateMatchConfidences(donorLookupResults, patientLookupResults, gradingResults);

            confidences.DataAtPosition(Locus, Position).Should().Be(MatchConfidence.Potential);
        }
        
        [TestCase(typeof(AlleleStringScoringInfo))]
        [TestCase(typeof(XxCodeScoringInfo))]
        public void CalculateMatchConfidences_PatientMultiplePGroups_DonorSerology_ReturnsPotential(Type patientScoringInfoType)
        {
            var serologyEntries = new List<SerologyEntry>{new SerologyEntry("serology", SerologySubtype.Associated)};

            var donorLookupResults = new PhenotypeInfo<IHlaScoringLookupResult>();
            var donorLookupResult = new HlaScoringLookupResultBuilder()
                .WithHlaTypingCategory(HlaTypingCategory.Serology)
                .WithHlaScoringInfo(new SerologyScoringInfoBuilder().WithMatchingSerologies(serologyEntries).Build())
                .Build();
            donorLookupResults.SetAtLocus(Locus, Position, donorLookupResult);
            
            var patientLookupResults = new PhenotypeInfo<IHlaScoringLookupResult>();
            var patientLookupResult = BuildScoringLookupResultWithMultiplePGroups(patientScoringInfoType, serologyEntries: serologyEntries);
            patientLookupResults.SetAtLocus(Locus, Position, patientLookupResult);

            var gradingResults = new PhenotypeInfo<MatchGradeResult>();

            var confidences = confidenceService.CalculateMatchConfidences(donorLookupResults, patientLookupResults, gradingResults);

            confidences.DataAtPosition(Locus, Position).Should().Be(MatchConfidence.Potential);
        }
        
        private static HlaScoringLookupResult BuildScoringLookupResultWithSinglePGroup(Type scoringInfoType, string pGroupName = "single-p-group")
        {
            if (scoringInfoType == typeof(SingleAlleleScoringInfo))
            {
                return new HlaScoringLookupResultBuilder()
                    .WithHlaTypingCategory(HlaTypingCategory.Allele)
                    .WithHlaScoringInfo(new SingleAlleleScoringInfoBuilder().WithMatchingPGroup(pGroupName).Build())
                    .Build();
            }
            if (scoringInfoType == typeof(XxCodeScoringInfo))
            {
                return new HlaScoringLookupResultBuilder()
                    .WithHlaTypingCategory(HlaTypingCategory.XxCode)
                    .WithHlaScoringInfo(new XxCodeScoringInfoBuilder().WithMatchingPGroups(new List<string>{pGroupName}).Build())
                    .Build();
            }
            if (scoringInfoType == typeof(AlleleStringScoringInfo))
            {
                return new HlaScoringLookupResultBuilder()
                    .WithHlaTypingCategory(HlaTypingCategory.NmdpCode)
                    .WithHlaScoringInfo(new AlleleStringScoringInfoBuilder()
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
            IEnumerable<SerologyEntry> serologyEntries = null)
        {
            serologyEntries = serologyEntries ?? new List<SerologyEntry>();
            pGroupNames = pGroupNames ?? new List<string> {"p-group-1", "p-group-2"};
            if (scoringInfoType == typeof(XxCodeScoringInfo))
            {
                return new HlaScoringLookupResultBuilder()
                    .WithHlaTypingCategory(HlaTypingCategory.XxCode)
                    .WithHlaScoringInfo(new XxCodeScoringInfoBuilder()
                        .WithMatchingPGroups(pGroupNames)
                        .WithMatchingSerologies(serologyEntries)
                        .Build()
                    )
                    .Build();
            }

            if (scoringInfoType == typeof(AlleleStringScoringInfo))
            {
                return new HlaScoringLookupResultBuilder()
                    .WithHlaTypingCategory(HlaTypingCategory.NmdpCode)
                    .WithHlaScoringInfo(new AlleleStringScoringInfoBuilder()
                        .WithAlleleScoringInfos(pGroupNames.Select(p =>
                            new SingleAlleleScoringInfoBuilder().WithMatchingPGroup(p).WithMatchingSerologies(serologyEntries).Build()))
                        .Build()
                    )
                    .Build();
            }
            throw new Exception($"Unsupported type: {scoringInfoType}");
        }
    }
}