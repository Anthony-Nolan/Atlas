using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Atlas.Client.Models.Search.Results.Matching.PerLocus
{
    /// <summary>
    /// This enum represents the mismatch direction of a Dpb1Mismatch. 
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum MismatchDirection
    {
        /// <summary>
        /// The mismatch direction of the Dpb1Mismatch is not applicable.
        /// </summary>
        NotApplicable,

        /// <summary>
        /// A mismatch at this locus will not be tolerated, and will cause complications after transplantation.
        /// The directionality of the mismatch is HvG - aka "Host vs Graft"; the host patient's immune system will reject the donor's tissue.
        /// </summary>
        NonPermissiveHvG,

        /// <summary>
        /// A mismatch at this locus will not be tolerated, and will cause complications after transplantation.
        /// The directionality of the mismatch is GvH - aka "Graft vs Host"; the donor's tissue will attack the host patient.
        /// </summary>
        NonPermissiveGvH
    }
}