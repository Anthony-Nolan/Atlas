using Microsoft.WindowsAzure.Storage.Table;
using Nova.SearchAlgorithm.Common.Models;

namespace Nova.SearchAlgorithm.Repositories.Donors.AzureStorage
{
    public class DonorTableEntity : TableEntity
    {
        public int DonorId { get; set; }
        public int DonorType { get; set; }
        public int RegistryCode { get; set; }

        public string A_1 { get; set; }
        public string A_2 { get; set; }
        public string B_1 { get; set; }
        public string B_2 { get; set; }
        public string C_1 { get; set; }
        public string C_2 { get; set; }
        public string DQB1_1 { get; set; }
        public string DQB1_2 { get; set; }
        public string DRB1_1 { get; set; }
        public string DRB1_2 { get; set; }

        public DonorTableEntity() { }

        public DonorTableEntity(string partitionKey, string rowKey) : base(partitionKey, rowKey) { }
    }
}