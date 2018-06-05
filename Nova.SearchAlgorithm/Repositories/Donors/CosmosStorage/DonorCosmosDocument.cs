using System;
using Newtonsoft.Json;
using Nova.SearchAlgorithm.Client.Models;
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

        public static DonorCosmosDocument FromInputDonor(InputDonor input)
        {
            return new DonorCosmosDocument
            {
                Id = input.DonorId.ToString(),
                RegistryCode = input.RegistryCode,
                DonorType = input.DonorType,
                A_1 = input.MatchingHla?.A_1?.Name,
                A_2 = input.MatchingHla?.A_2?.Name,
                B_1 = input.MatchingHla?.B_1?.Name,
                B_2 = input.MatchingHla?.B_2?.Name,
                C_1 = input.MatchingHla?.C_1?.Name,
                C_2 = input.MatchingHla?.C_2?.Name,
                Drb1_1 = input.MatchingHla?.DRB1_1?.Name,
                Drb1_2 = input.MatchingHla?.DRB1_2?.Name,
                Dqb1_1 = input.MatchingHla?.DQB1_1?.Name,
                Dqb1_2 = input.MatchingHla?.DQB1_2?.Name,
            };
        }
    }
}