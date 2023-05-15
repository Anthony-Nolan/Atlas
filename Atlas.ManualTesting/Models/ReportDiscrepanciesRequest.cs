namespace Atlas.ManualTesting.Models
{
    public class ReportDiscrepanciesRequest
    {
        /// <summary>
        /// Path to file with consensus results
        /// </summary>
        public string ConsensusFilePath { get; set; }

        /// <summary>
        /// Path to file with Atlas results that should be compared to the <see cref="ConsensusFilePath"/>
        /// </summary>
        public string ResultsFilePath { get; set; }

        /// <summary>
        /// Path to file containing patient HLA
        /// </summary>
        public string PatientFilePath { get; set; }

        /// <summary>
        /// Path to file containing donor HLA
        /// </summary>
        public string DonorFilePath { get; set; }
    }
}