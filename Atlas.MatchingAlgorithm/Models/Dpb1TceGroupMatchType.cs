using Atlas.Client.Models.Common.Results;

namespace Atlas.MatchingAlgorithm.Models
{
    /// <summary>
    /// Represents the match type of a pair of donor/patient loci, based *only* on the corresponding TCE groups.
    ///
    /// Similar to <see cref="MatchCategory.PermissiveMismatch"/> - though in the latter's case, we know that the loci are otherwise a mismatch,
    /// and so can call it a "Permissive Mismatch".
    ///
    /// If the loci are otherwise a match, the TCE group match type will not be relevant.
    /// e.g. it is possible to have a "permissive" TCE group match type, despite the loci otherwise matching - so this value will be
    /// "Permissive", while the locus overall would just be considered a "Match" 
    /// </summary>
    public enum Dpb1TceGroupMatchType
    {
        /// <summary>
        /// A mismatch at this locus may be tolerated (aka "permitted") after transplantation.
        /// </summary>
        Permissive,

        /// <summary>
        /// A mismatch at this locus will not be tolerated, and will cause complications after transplantation.
        /// The directionality of the mismatch is HvG - aka "Host vs Graft"; the host patient's immune system will reject the donor's tissue.
        /// </summary>
        NonPermissiveHvG,

        /// <summary>
        /// A mismatch at this locus will not be tolerated, and will cause complications after transplantation.
        /// The directionality of the mismatch is GvH - aka "Graft vs Host"; the donor's tissue will attack the host patient.
        /// </summary>
        NonPermissiveGvH,

        /// <summary>
        /// The TCE group based match type could not be calculated - either the patient or donor is untyped at DPB1, or one of the alleles involved has
        /// no known TCE group. 
        /// </summary>
        Unknown,
    }
}