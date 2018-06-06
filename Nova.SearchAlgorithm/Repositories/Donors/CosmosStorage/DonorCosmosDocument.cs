using Newtonsoft.Json;
using Nova.SearchAlgorithm.Common.Models;
using Nova.SearchAlgorithm.Data.Models;
using Nova.SearchAlgorithm.Exceptions;

namespace Nova.SearchAlgorithm.Repositories.Donors.CosmosStorage
{
    /// <summary>
    /// A donor from our data source along with the donor's raw hla data.
    /// </summary>
    public class DonorCosmosDocument
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
        public DonorType DonorType { get; set; }
        public RegistryCode RegistryCode { get; set; }

        public string A_1 { get; set; }
        public string A_2 { get; set; }
        public string B_1 { get; set; }
        public string B_2 { get; set; }
        public string C_1 { get; set; }
        public string C_2 { get; set; }
        public string Drb1_1 { get; set; }
        public string Drb1_2 { get; set; }
        public string Dqb1_1 { get; set; }
        public string Dqb1_2 { get; set; }

        public DonorResult ToDonorResult()
        {
            if (!int.TryParse(Id, out int donorId))
            {
                throw new CloudStorageException("A donor in CosmosDB seems to have a non-integer ID: " + Id);
            }

            return new DonorResult
            {
                DonorId = donorId,
                DonorType = DonorType,
                RegistryCode = RegistryCode,
                HlaNames = new PhenotypeInfo<string>
                {
                    A_1 = A_1,
                    A_2 = A_2,
                    B_1 = B_1,
                    B_2 = B_2,
                    C_1 = C_1,
                    C_2 = C_2,
                    DQB1_1 = Dqb1_1,
                    DQB1_2 = Dqb1_2,
                    DRB1_1 = Drb1_1,
                    DRB1_2 = Drb1_2
                }
            };
        }

        public static DonorCosmosDocument FromRawInputDonor(RawInputDonor input)
        {
            return new DonorCosmosDocument
            {
                Id = input.DonorId.ToString(),
                RegistryCode = input.RegistryCode,
                DonorType = input.DonorType,
                A_1 = input.HlaNames?.A_1,
                A_2 = input.HlaNames?.A_2,
                B_1 = input.HlaNames?.B_1,
                B_2 = input.HlaNames?.B_2,
                C_1 = input.HlaNames?.C_1,
                C_2 = input.HlaNames?.C_2,
                Drb1_1 = input.HlaNames?.DRB1_1,
                Drb1_2 = input.HlaNames?.DRB1_2,
                Dqb1_1 = input.HlaNames?.DQB1_1,
                Dqb1_2 = input.HlaNames?.DQB1_2
            };
        }
    }
}