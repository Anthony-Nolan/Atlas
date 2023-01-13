using System;
using System.Threading.Tasks;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
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
            (
                valueA: new LocusInfo<string>("*02:01:84", "*01:01"),
                valueB: new LocusInfo<string>("*15:01", "*15:01"),
                valueDpb1: new LocusInfo<string>(donorDpb1, donorDpb1),
                valueDrb1: new LocusInfo<string>("*15:03", "*15:03")
            );
            var patientHla = new PhenotypeInfo<string>
            (
                valueA: new LocusInfo<string>(donorHla.A.Position1, donorHla.A.Position2),
                valueB: new LocusInfo<string>(donorHla.B.Position1, donorHla.B.Position2),
                valueDpb1: new LocusInfo<string>(patientDpb1, patientDpb1),
                valueDrb1: new LocusInfo<string>(donorHla.Drb1.Position1, donorHla.Drb1.Position2)
            );

            await SpecificTestDataSteps.GivenDonorAndPatientHla(donorHla, patientHla, scenarioContext);
        }
    }
}