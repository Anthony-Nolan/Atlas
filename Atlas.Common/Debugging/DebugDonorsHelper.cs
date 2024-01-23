using System;
using System.Collections.Generic;
using System.Linq;
using Atlas.Client.Models.Debug;

namespace Atlas.Common.Debugging
{
    public static class DebugDonorsHelper
    {
        public static DebugDonorsResult<TDonor> BuildDebugDonorsResult<TDonor>(
            IReadOnlyCollection<string> externalDonorCodes,
            IReadOnlyCollection<TDonor> presentDonors,
            Func<TDonor, string> idSelector)
        {
            var distinctCodes = externalDonorCodes.Distinct().ToList();
            var absentDonors = distinctCodes.Except(presentDonors.Select(idSelector)).ToList();

            return new DebugDonorsResult<TDonor>
            {
                DonorCounts = new DebugDonorsResult<TDonor>.Counts
                {
                    Absent = absentDonors.Count,
                    Present = presentDonors.Count,
                    Received = distinctCodes.Count
                },
                AbsentDonors = absentDonors,
                PresentDonors = presentDonors,
                ReceivedDonors = distinctCodes
            };
        }
    }
}