using Atlas.Common.GeneticData.PhenotypeInfo.TransferModels;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;

namespace Atlas.MatchPrediction.Test.Verification.Models
{
    public class GenerateTestHarnessRequest
    {
        public MaskingRequestsTransfer PatientMaskingRequests { get; set; }
        public MaskingRequestsTransfer DonorMaskingRequests { get; set; }
    }

    /// <summary>
    /// Multiple masking requests may be submitted per locus.
    /// The final sum of typings proportions per locus must be between 0 to 100%, inclusive.
    /// </summary>
    public class MaskingRequestsTransfer : LociInfoTransfer<IEnumerable<MaskingRequest>>
    {
    }

    public class MaskingRequest
    {
        public MaskingCategory MaskingCategory { get; set; }

        /// <summary>
        /// Percentage (to nearest whole integer) of locus typings to mask to category of <see cref="MaskingCategory"/>.
        /// </summary>
        public int ProportionToMask { get; set; }
    }

    /// <summary>
    /// Currently available options for the masking of genotype HLA.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum MaskingCategory
    {
        /// <summary>
        /// If genotype has >2 fields, truncates typing to its first two fields (does not preserve expression letters)
        /// </summary>
        TwoField,

        PGroup,

        /// <summary>
        /// a.k.a. "NMDP code"
        /// </summary>
        MultipleAlleleCode,

        XxCode,
        Serology,

        /// <summary>
        /// Delete the locus typing - not permitted at "required" matching loci.
        /// </summary>
        Delete
    }
}