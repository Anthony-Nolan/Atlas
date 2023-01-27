using System;
using System.Collections.Generic;
using System.Linq;

namespace Atlas.Common.Debugging.Donors
{
    public static class DebugDonorsHelper
    {
        public static DebugDonorsResult<TDonor, TDonorId> BuildDebugDonorsResult<TDonor, TDonorId>(
            IReadOnlyCollection<TDonorId> allDonorIds,
            IReadOnlyCollection<TDonor> presentDonors,
            Func<TDonor, TDonorId> idSelector)
        {
            var absentDonorIds = allDonorIds.Except(presentDonors.Select(idSelector)).ToList();

            return new DebugDonorsResult<TDonor, TDonorId>
            {
                DonorCounts = new DebugDonorsResult<TDonor, TDonorId>.Counts
                {
                    Absent = absentDonorIds.Count,
                    Present = presentDonors.Count,
                    Received = allDonorIds.Count
                },
                AbsentDonorIds = absentDonorIds,
                PresentDonors = presentDonors,
                ReceivedDonorIds = allDonorIds
            };
        }
    }
}
