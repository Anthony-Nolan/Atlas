using Atlas.Common.Public.Models.GeneticData.PhenotypeInfo.TransferModels;

namespace Atlas.Client.Models.Scoring
{
    public class IdentifiedDonorHla : PhenotypeInfoTransfer<string>
    {
        public string DonorId { get; set; }
    }
}
