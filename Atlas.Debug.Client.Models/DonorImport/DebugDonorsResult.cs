﻿using System.Collections.Generic;

namespace Atlas.Debug.Client.Models.DonorImport
{
    public class DebugDonorsResult<TDonor>
    {
        public Counts DonorCounts { get; set; }

        /// <summary>
        /// Codes of donors that were NOT found in the donor store
        /// </summary>
        public IEnumerable<string> AbsentDonors { get; set; }

        /// <summary>
        /// Info of donors that were found in the donor store
        /// </summary>
        public IEnumerable<TDonor> PresentDonors { get; set; }

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