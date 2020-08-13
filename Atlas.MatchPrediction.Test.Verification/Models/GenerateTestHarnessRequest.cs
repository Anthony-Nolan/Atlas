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
        PGroup,

        // a.k.a. "NMDP code"
        MultipleAlleleCode,

        XxCode,
        Serology,

        // Request to delete locus typings - not permitted at "required" matching loci.
        Delete
    }
}