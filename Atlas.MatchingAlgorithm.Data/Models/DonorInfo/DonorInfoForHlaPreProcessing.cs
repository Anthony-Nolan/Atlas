using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo;

namespace Atlas.MatchingAlgorithm.Data.Models.DonorInfo
{
    /// <summary>
    ///  To be used only for donor HLA processing data entry
    /// </summary>
    public class DonorInfoForHlaPreProcessing
    {
        public int DonorId { get; set; }
        public PhenotypeInfo<int?> HlaNameIds { get; set; }
    }
}