using System.Collections.Generic;
using System.Linq;
using Atlas.Debug.Client.Models.DonorImport;

namespace Atlas.Common.Debugging
{
    public static class DebugDonorsHelper
    {
        public static DebugDonorsResult BuildDebugDonorsResult(
            IReadOnlyCollection<string> externalDonorCodes,
            IReadOnlyCollection<DonorDebugInfo> presentDonors)
        {
            var distinctCodes = externalDonorCodes.Distinct().ToList();
            var absentDonors = distinctCodes.Except(presentDonors.Select(d => d.ExternalDonorCode)).ToList();

            return new DebugDonorsResult
            {
                DonorCounts = new DebugDonorsResult.Counts
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