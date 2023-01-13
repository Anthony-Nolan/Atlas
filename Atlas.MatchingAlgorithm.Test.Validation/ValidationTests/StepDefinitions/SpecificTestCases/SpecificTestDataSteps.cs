using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;
using Atlas.MatchingAlgorithm.Client.Models.Donors;
using Atlas.MatchingAlgorithm.Data.Models.DonorInfo;
using Atlas.MatchingAlgorithm.Test.Validation.DependencyInjection;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Repositories;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Services;
using Atlas.MatchingAlgorithm.Test.Validation.TestData.Services.PatientDataSelection.StaticDataSelection;
using TechTalk.SpecFlow;
using Microsoft.Extensions.DependencyInjection;

namespace Atlas.MatchingAlgorithm.Test.Validation.ValidationTests.StepDefinitions.SpecificTestCases
{
    internal static class SpecificTestDataSteps
    {
        public static async Task GivenDonorHla(PhenotypeInfo<string> donorHla, ScenarioContext scenarioContext)
        {
            var staticDataProvider = scenarioContext.Get<IStaticDataProvider>();
            var donorInfo = new DonorInfo
            {
                DonorId = DonorIdGenerator.NextId(),
                HlaNames = donorHla,
                DonorType = DonorType.Adult
            };

            await AddDonors(new[] {donorInfo});

            staticDataProvider.SetExpectedDonorIds(new[] {donorInfo.DonorId});

            scenarioContext.Set(staticDataProvider);
            scenarioContext.Set((IExpectedDonorProvider) staticDataProvider);
        }

        private static async Task AddDonors(IReadOnlyCollection<DonorInfo> donors)
        {
            await AlgorithmTestingService.AddDonors(donors);

            var testDataRepo = ServiceConfiguration.Provider.GetService<ITestDataRepository>();
            testDataRepo.AddDonorsToAtlasDonorStore(donors.Select(d => d.DonorId));
        }

        public static async Task GivenDonorAndPatientHla(
            PhenotypeInfo<string> donorHla,
            PhenotypeInfo<string> patientHla,
            ScenarioContext scenarioContext)
        {
            await GivenDonorHla(donorHla, scenarioContext);
            GivenPatientHla(patientHla, scenarioContext);
        }

        public static void GivenPatientHla(PhenotypeInfo<string> patientHla, ScenarioContext scenarioContext)
        {
            var staticDataProvider = scenarioContext.Get<IStaticDataProvider>();

            staticDataProvider.SetPatientHla(patientHla);

            scenarioContext.Set(staticDataProvider);
            scenarioContext.Set((IPatientDataProvider) staticDataProvider);
        }
    }
}