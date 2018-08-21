using System.Collections.Generic;

namespace Nova.SearchAlgorithm.Test.Validation.TestData.Services.PatientDataSelection
{
    /// <summary>
    /// Used to specify multiple patients with the same criteria.
    /// Each patient specified should match a unique meta-donor.
    /// </summary>
    public interface IMultiplePatientDataSelector
    {
        List<IPatientDataSelector> PatientDataSelectors { get; set; }
    }

    public class MultiplePatientDataSelector: IMultiplePatientDataSelector
    {
        public List<IPatientDataSelector> PatientDataSelectors { get; set; } = new List<IPatientDataSelector>();

        public MultiplePatientDataSelector(
            IMetaDonorSelector metaDonorSelector,
            IDatabaseDonorSelector databaseDonorSelector,
            IPatientHlaSelector patientHlaSelector)
        {
            for (var i = 0; i < 50; i++)
            {
                PatientDataSelectors.Add(new PatientDataSelector(metaDonorSelector, databaseDonorSelector, patientHlaSelector));
            }
        }
    }
}