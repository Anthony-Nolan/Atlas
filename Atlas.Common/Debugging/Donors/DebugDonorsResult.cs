using System.Collections.Generic;

namespace Atlas.Common.Debugging.Donors
{
    public class DebugDonorsResult<TDonor, TDonorId>
    {
        public Counts DonorCounts { get; set; }

        /// <summary>
        /// Ids of donors that were NOT found in the donor store
        /// </summary>
        public IEnumerable<TDonorId> AbsentDonorIds { get; set; }

        /// <summary>
        /// Info of donors that were found in the donor store
        /// </summary>
        public IEnumerable<TDonor> PresentDonors { get; set; }

        /// <summary>
        /// List of all ids provided in the debug request
        /// </summary>
        public IEnumerable<TDonorId> ReceivedDonorIds { get; set; }

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
