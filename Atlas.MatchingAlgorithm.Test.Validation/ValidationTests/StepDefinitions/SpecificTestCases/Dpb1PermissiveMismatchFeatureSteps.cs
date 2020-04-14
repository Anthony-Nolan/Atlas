using Atlas.MatchingAlgorithm.Common.Models;
using System;
using System.Threading.Tasks;
using TechTalk.SpecFlow;

namespace Atlas.MatchingAlgorithm.Test.Validation.ValidationTests.StepDefinitions.SpecificTestCases
{
    [Binding]
    public class Dpb1PermissiveMismatchFeatureSteps
    {
        private readonly ScenarioContext scenarioContext;
        
        public Dpb1PermissiveMismatchFeatureSteps(ScenarioContext scenarioContext)
        {
            this.scenarioContext = scenarioContext;
        }
        
        [Given(@"the patient and donor have mismatched DPB1 alleles with (.*) TCE group assignments")]
        public async Task GivenPatientAndDonorWithMismatchedDpb1Alleles(string tceGroupAssignments)
        {
            const string donorDpb1FromTceGroup3 = "01:01:01:01";
            const string donorDpb1WithoutTceGroupAssignment = "680:01";
            const string patientDpb1FromTceGroup3 = "02:01:02:01";
            const string patientDpb1FromTceGroup2 = "03:01:01:01";
            const string patientDpb1WithoutTceGroupAssignment = "701:01";

            string donorDpb1;
            string patientDpb1;

            switch (tceGroupAssignments)
            {
                case "the same":
                    donorDpb1 = donorDpb1FromTceGroup3;
                    patientDpb1 = patientDpb1FromTceGroup3;
                    break;
                case "different":
                    donorDpb1 = donorDpb1FromTceGroup3;
                    patientDpb1 = patientDpb1FromTceGroup2;
                    break;
                case "no":
                    donorDpb1 = donorDpb1WithoutTceGroupAssignment;
                    patientDpb1 = patientDpb1WithoutTceGroupAssignment;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            var donorHla = new PhenotypeInfo<string>
            {
                A = {Position1 = "*02:01:84", Position2 = "*01:01"},
                B = {Position1 = "*15:01", Position2 = "*15:01"},
                Dpb1 = { Position1 = donorDpb1, Position2 = donorDpb1 },
                Drb1 = {Position1 = "*15:03", Position2 = "*15:03"}
            };
            var patientHla = new PhenotypeInfo<string>
            {
                A = {Position1 = donorHla.A.Position1, Position2 = donorHla.A.Position2},
                B = {Position1 = donorHla.B.Position1, Position2 = donorHla.B.Position2},
                Dpb1 = { Position1 = patientDpb1, Position2 = patientDpb1 },
                Drb1 = {Position1 = donorHla.Drb1.Position1, Position2 = donorHla.Drb1.Position2}
            };

            await SpecificTestDataSteps.GivenDonorAndPatientHla(donorHla, patientHla, scenarioContext);
        }
    }
}