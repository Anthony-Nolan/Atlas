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

        public PhenotypeInfo<string> HlaNames { get; set; }
        public PhenotypeInfo<ExpandedHla> MatchingHla { get; set; }

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
                HlaNames = HlaNames
            };
        }

        public static DonorCosmosDocument FromRawInputDonor(RawInputDonor input)
        {
            return new DonorCosmosDocument
            {
                Id = input.DonorId.ToString(),
                RegistryCode = input.RegistryCode,
                DonorType = input.DonorType,
                HlaNames = input.HlaNames
            };
        }

        public static DonorCosmosDocument FromInputDonor(InputDonor input)
        {
            return new DonorCosmosDocument
            {
                Id = input.DonorId.ToString(),
                RegistryCode = input.RegistryCode,
                DonorType = input.DonorType,
                HlaNames = input.ToRawInputDonor().HlaNames,
                MatchingHla = input.MatchingHla
            };
        }
    }
}