using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Test.Validation.TestData.Models.Hla;

namespace Nova.SearchAlgorithm.Test.Validation.TestData.Models.PatientDataSelection
{
    /// <summary>
    /// A set of criteria used to select a database donor
    /// (i.e. a 'Donor' in algorithm terminology. 'Database Donor' used to distinguish from 'Meta-Donor's)
    /// </summary>
    public class DatabaseDonorSelectionCriteria
    {
        /// <summary>
        /// Determines to what resolution the expected matched donor is typed
        /// </summary>
        public readonly PhenotypeInfo<HlaTypingResolution> MatchingTypingResolutions = new PhenotypeInfo<HlaTypingResolution>();
    }
}