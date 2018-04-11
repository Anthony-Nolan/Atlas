using Microsoft.WindowsAzure.Storage.Table;
using Nova.SearchAlgorithm.Client.Models;
using Nova.SearchAlgorithm.Models;

namespace Nova.SearchAlgorithm.Repositories.Donors.AzureStorage
{
    public class DonorTableEntity : TableEntity
    {
        public string SerialisedDonor { get; set; }

        public int DonorId { get; set; }
        public string DonorType { get; set; }
        public RegistryCode RegistryCode { get; set; }
        public PhenotypeInfo<MatchingHla> MatchingHla { get; set; }

        public DonorTableEntity() { }

        public DonorTableEntity(string partitionKey, string rowKey) : base(partitionKey, rowKey) { }
    }
}