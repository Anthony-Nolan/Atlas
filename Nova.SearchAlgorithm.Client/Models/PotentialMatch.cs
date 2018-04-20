using Nova.SearchAlgorithm.Client.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Nova.SearchAlgorithm.Client.Models
{
    public class PotentialMatch
    {
        /// <summary>
        /// The ID of the donor for lookup in donor registries.
        /// </summary>
        public int DonorId { get; set; }
        
        // TODO:NOVA-924 make donor type a strongly typed Enum
        /// <summary>
        /// The type of donor, for example Adult or Cord
        /// </summary>
        public string DonorType { get; set; }

        /// <summary>
        /// The code of the donor registry which this donor originates from.
        /// </summary>
        public RegistryCode Registry { get; set; }

        /// <summary>
        /// The number of loci matched, down to the type.
        /// Out of a maximum of 10.
        /// </summary>
        public int TotalMatchCount { get; set; }

        /// <summary>
        /// The number of Loci which are typed for this donor.
        /// </summary>
        public int TypedLociCount { get; set; }

        /// <summary>
        /// The details of the match at each locus A.
        /// </summary>
        public LocusMatchDetails MatchDetailsAtLocusA { get; set; }

        /// <summary>
        /// The details of the match at each locus B.
        /// </summary>
        public LocusMatchDetails MatchDetailsAtLocusB { get; set; }

        /// <summary>
        /// The details of the match at each locus C.
        /// </summary>
        public LocusMatchDetails MatchDetailsAtLocusC { get; set; }

        /// <summary>
        /// The details of the match at each locus DRB1.
        /// </summary>
        public LocusMatchDetails MatchDetailsAtLocusDRB1 { get; set; }

        /// <summary>
        /// The details of the match at each locus DQB1.
        /// </summary>
        public LocusMatchDetails MatchDetailsAtLocusDQB1 { get; set; }

        // TODO: NOVA-924 Do we need to include the (original) phenotype,
        // or can the search client retrieve that from the original registry?
        // TODO: NOVA-930 add fields for sorting such as birth date, gender, TNC final count.
    }
}