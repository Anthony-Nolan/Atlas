using System.Collections.Generic;
using Nova.SearchAlgorithm.Common.Models;

namespace Nova.SearchAlgorithm.Test.Validation.TestData.Services.PatientDataSelection
{
    public interface IStaticDataProvider
    {
        void SetExpectedDonorIds(IEnumerable<int> donorIds);
        void SetPatientHla(PhenotypeInfo<string> hla);
    }
    
    public class StaticDataProvider: IStaticDataProvider, IExpectedDonorProvider, IPatientDataProvider
    {
        private IEnumerable<int> expectedDonorIds;
        private PhenotypeInfo<string> patientHla;

        public void SetExpectedDonorIds(IEnumerable<int> donorIds)
        {
            expectedDonorIds = donorIds;
        }

        public void SetPatientHla(PhenotypeInfo<string> hla)
        {
            patientHla = hla;
        }

        public PhenotypeInfo<string> GetPatientHla()
        {
            return patientHla;
        }

        public IEnumerable<int> GetExpectedMatchingDonorIds()
        {
            return expectedDonorIds;
        }
    }
}