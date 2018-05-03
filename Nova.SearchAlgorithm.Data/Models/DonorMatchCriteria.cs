using Nova.SearchAlgorithm.Client.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Nova.SearchAlgorithm.Data.Models
{
    public class DonorMatchCriteria
    {
        public DonorType SearchType { get; set; }
        public IEnumerable<RegistryCode> RegistriesToSearch { get; set; }

        public int DonorMismatchCountTier1 { get; set; }
        public int? DonorMismatchCountTier2 { get; set; }

        public DonorLocusMatchCriteria LocusMismatchA { get; set; }
        public DonorLocusMatchCriteria LocusMismatchB { get; set; }
        public DonorLocusMatchCriteria LocusMismatchC { get; set; }
        public DonorLocusMatchCriteria LocusMismatchDQB1 { get; set; }
        public DonorLocusMatchCriteria LocusMismatchDRB1 { get; set; }
    }

    // TODO: rename, too similar to LocusMismatchCriteria
    public class DonorLocusMatchCriteria
    {
        public int MismatchCount { get; set; }
        public IEnumerable<string> HlaNamesToMatchInPositionOne { get; set; }
        public IEnumerable<string> HlaNamesToMatchInPositionTwo { get; set; }
    }
}