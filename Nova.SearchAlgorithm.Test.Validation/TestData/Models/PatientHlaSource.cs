using System.Text.RegularExpressions;

namespace Nova.SearchAlgorithm.Test.Validation.TestData.Models
{
    /// <summary>
    /// Determines what data will be used to select a patient's hla 
    /// </summary>
    public enum PatientHlaSource
    {
        /// <summary>
        /// Patient HLA will be selected to match the selected donor
        /// </summary>
        Match,
        /// <summary>
        /// An single expressing allele will be used, that does not match the corresponding donor HLA
        /// </summary>
        ExpressingAlleleMismatch,
        /// <summary>
        /// A single null allele will be selected, that does not match the corresponding donor HLA, even when donor HLA also null
        /// </summary>
        NullAlleleMismatch,
    }
}