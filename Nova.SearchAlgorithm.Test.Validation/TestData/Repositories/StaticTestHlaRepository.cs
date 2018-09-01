using System;
using System.Collections.Generic;
using System.Linq;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Data.Entity;
using Nova.SearchAlgorithm.Test.Validation.TestData.Services;
using Nova.SearchAlgorithm.Test.Validation.TestData.Services.PatientDataSelection;

namespace Nova.SearchAlgorithm.Test.Validation.TestData.Repositories
{
    public interface IStaticTestHlaRepository
    {
        PhenotypeInfo<string> GetPatientHlaData(StaticDataTestCase testCase);

        IEnumerable<Donor> GetAllDonors();
    }

    public class StaticTestHlaRepository : IStaticTestHlaRepository
    {
        public PhenotypeInfo<string> GetPatientHlaData(StaticDataTestCase testCase)
        {
            switch (testCase)
            {
                case StaticDataTestCase.MatchingDonorsAtEachMatchGrade:
                    return Resources.SpecificTestCases.HlaData.AllMatchGrades.PatientHla;
                default:
                    throw new ArgumentOutOfRangeException(nameof(testCase), testCase, null);
            }
        }

        public IEnumerable<Donor> GetAllDonors()
        {
            var testDataSets = new[] {Resources.SpecificTestCases.HlaData.AllMatchGrades.DonorHlaSets};

            return testDataSets
                .SelectMany(x => x)
                .Select(hla => new Donor
                {
                    DonorId = DonorIdGenerator.NextId(),
                    DonorType = DonorType.Adult,
                    RegistryCode = RegistryCode.AN,
                    A_1 = hla.A_1,
                    A_2 = hla.A_2,
                    B_1 = hla.B_1,
                    B_2 = hla.B_2,
                    C_1 = hla.C_1,
                    C_2 = hla.C_2,
                    DPB1_1 = hla.DPB1_1,
                    DPB1_2 = hla.DPB1_2,
                    DQB1_1 = hla.DQB1_1,
                    DQB1_2 = hla.DQB1_2,
                    DRB1_1 = hla.DRB1_1,
                    DRB1_2 = hla.DRB1_2,
                });
        }
    }
}