namespace Atlas.ManualTesting.Models
{
    public class ImportAndScoreRequest
    {
        /// <summary>
        /// Path to file containing patient HLA
        /// </summary>
        public string PatientFilePath { get; set; }

        /// <summary>
        /// Path to file containing donor HLA
        /// </summary>
        public string DonorFilePath { get; set; }

        /// <summary>
        /// Path to file where results should be written
        /// </summary>
        public string ResultsFilePath { get; set; }

        /// <summary>
        /// In case the scoring request is interrupted, it can be restarted from a later patient id.
        /// If not set, then scoring will commence from the start of the patient collection.
        /// </summary>
        public string StartFromPatientId { get; set; }

        /// <summary>
        /// In case the scoring request is interrupted, it can be restarted from a later donor id for the patient listed in <see cref="StartFromPatientId"/>.
        /// The ID will only be applied to the first patient processed, thereafter the entire donor collection will be scored.
        /// If not set, then scoring will commence from the start of the donor collection.
        /// </summary>
        public string StartFromDonorId { get; set; }
    }
}
