using System.Collections.Generic;

namespace Atlas.Debug.Client.Models.DonorImport
{
    /// <summary>
    /// Result of a debug request checking for presence or absence of a list of donors in the target database.
    /// </summary>
    public class DebugDonorsResult
    {
        public Counts DonorCounts { get; set; }

        /// <summary>
        /// Codes of donors that were NOT found in the donor store
        /// </summary>
        public IEnumerable<string> AbsentDonors { get; set; }

        /// <summary>
        /// Info of donors that were found in the donor store
        /// </summary>
        public IEnumerable<DonorDebugInfo> PresentDonors { get; set; }

        /// <summary>
        /// List of all donor codes provided in the debug request
        /// </summary>
        public IEnumerable<string> ReceivedDonors { get; set; }

        public class Counts
        {
            /// <summary>
            /// Count of donors NOT found in the donor store
            /// </summary>
            public int Absent { get; set; }

            /// <summary>
            /// Count of donors that were found in the donor store
            /// </summary>
            public int Present { get; set; }

            /// <summary>
            /// Count of all donor Ids provided in the debug request
            /// </summary>
            public int Received { get; set; }
        }
    }
}
