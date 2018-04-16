using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Nova.SearchAlgorithm.Client.Models
{
    // TODO:NOVA-924 could we just replace this class with a nullable integer?
    public class LocusMatchDetails
    {
        /// <summary>
        /// Reports whether this locus is typed for the given donor.
        /// </summary>
        public bool IsLocusTyped { get; set; }

        /// <summary>
        /// The number of matches within this locus.
        /// Either 0, 1 or 2 if the locus is typed.
        /// Null if the locus is not typed.
        /// </summary>
        public int? MatchCount { get; set; }
    }
}