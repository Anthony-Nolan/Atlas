using System.Collections.Generic;

namespace Nova.SearchAlgorithm.Test.Validation.TestData.Services.PatientDataSelection.PatientFactories
{
    /// <summary>
    /// Used to specify multiple patients with the same criteria.
    /// Each patient specified should match a unique meta-donor.
    /// </summary>
    public interface IMultiplePatientDataFactory
    {
        void SetNumberOfPatients(int numberOfPatients);
        List<IPatientDataFactory> PatientDataFactories { get; set; }
    }

    public class MultiplePatientDataFactory: IMultiplePatientDataFactory
    {
        private const int DefaultNumberOfPatients = 50;

        private readonly IMetaDonorSelector metaDonorSelector;
        private readonly IDatabaseDonorSelector databaseDonorSelector;
        private readonly IPatientHlaSelector patientHlaSelector;

        public List<IPatientDataFactory> PatientDataFactories { get; set; } = new List<IPatientDataFactory>();

        public MultiplePatientDataFactory(
            IMetaDonorSelector metaDonorSelector,
            IDatabaseDonorSelector databaseDonorSelector,
            IPatientHlaSelector patientHlaSelector)
        {
            this.metaDonorSelector = metaDonorSelector;
            this.databaseDonorSelector = databaseDonorSelector;
            this.patientHlaSelector = patientHlaSelector;
            SetNumberOfPatients(DefaultNumberOfPatients);
        }

        /// <summary>
        /// Overrides the default number of patient selectors with a new collection of a given length
        /// Do not call this after modifying any of the selectors, as it will replace all existing selectors!
        /// </summary>
        public void SetNumberOfPatients(int numberOfPatients)
        {
            PatientDataFactories.Clear();
            for (var i = 0; i < numberOfPatients; i++)
            {
                var patientDataFactory = new PatientDataFactory(metaDonorSelector, databaseDonorSelector, patientHlaSelector);
                patientDataFactory.SetNumberOfMetaDonorsToSkip(i);
                PatientDataFactories.Add(patientDataFactory);
            }
        }
    }
}