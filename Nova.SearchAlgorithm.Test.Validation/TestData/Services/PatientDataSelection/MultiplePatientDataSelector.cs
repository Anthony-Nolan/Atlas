using System.Collections.Generic;

namespace Nova.SearchAlgorithm.Test.Validation.TestData.Services.PatientDataSelection
{
    /// <summary>
    /// Used to specify multiple patients with the same criteria.
    /// Each patient specified should match a unique meta-donor.
    /// </summary>
    public interface IMultiplePatientDataSelector
    {
        void SetNumberOfPatients(int numberOfPatients);
        List<SingleDonorPatientDataSelector> PatientDataSelectors { get; set; }
    }

    public class MultiplePatientDataSelector: IMultiplePatientDataSelector
    {
        private const int DefaultNumberOfPatients = 50;

        private readonly IMetaDonorSelector metaDonorSelector;
        private readonly IDatabaseDonorSelector databaseDonorSelector;
        private readonly IPatientHlaSelector patientHlaSelector;

        public List<SingleDonorPatientDataSelector> PatientDataSelectors { get; set; } = new List<SingleDonorPatientDataSelector>();

        public MultiplePatientDataSelector(
            IMetaDonorSelector metaDonorSelector,
            IDatabaseDonorSelector databaseDonorSelector,
            IPatientHlaSelector patientHlaSelector)
        {
            this.metaDonorSelector = metaDonorSelector;
            this.databaseDonorSelector = databaseDonorSelector;
            this.patientHlaSelector = patientHlaSelector;
            for (var i = 0; i < DefaultNumberOfPatients; i++)
            {
                PatientDataSelectors.Add(new SingleDonorPatientDataSelector(metaDonorSelector, databaseDonorSelector, patientHlaSelector));
            }
        }

        /// <summary>
        /// Overrides the default number of patient selectors with a new collection of a given length
        /// Do not call this after modifying any of the selectors, as it will replace all existing selectors!
        /// </summary>
        public void SetNumberOfPatients(int numberOfPatients)
        {
            PatientDataSelectors.Clear();
            for (var i = 0; i < numberOfPatients; i++)
            {
                PatientDataSelectors.Add(new SingleDonorPatientDataSelector(metaDonorSelector, databaseDonorSelector, patientHlaSelector));
            }
        }
    }
}