using System.Collections.Generic;
using Atlas.Common.GeneticData.Hla.Models;
using Atlas.Common.GeneticData.PhenotypeInfo.TransferModels;

namespace Atlas.MatchPrediction.Test.Verification.Models
{
    public class GenerateTestHarnessRequest
    {
        public LociInfoTransfer<MaskingCriterion> PatientMaskingCriteria { get; set; }
        public LociInfoTransfer<MaskingCriterion> DonorMaskingCriteria { get; set; }
    }

    public class MaskingCriterion
    {
        /// <summary>
        /// Percentage (to nearest whole integer) of locus typings to delete.
        /// </summary>
        public int ProportionToDelete { get; set; }

        /// <summary>
        /// Instructions for which typing categories the locus typings should be converted to, and to what proportions.
        ///  </summary>
        public IEnumerable<MaskingRequest> MaskingRequests { get; set; }
    }

    public class MaskingRequest
    {
        public HlaTypingCategory MaskHlaTypingCategory { get; set; }

        /// <summary>
        /// Percentage (to nearest whole integer) of locus typings to mask to typing category of <see cref="MaskHlaTypingCategory"/>.
        /// </summary>
        public int ProportionToMask { get; set; }
    }
}
