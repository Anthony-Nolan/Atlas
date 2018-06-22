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

        /// <summary>
        /// During the donor import we need to know how far we've got;
        /// the way to do this is to determine the highest donor ID that was inserted so far.
        /// Cosmos can't order by primary key so we duplicate the id here.
        /// https://stackoverflow.com/questions/48710600/azure-cosmosdb-how-to-order-by-id
        /// </summary>
        public int DuplicateIdForOrdering { get; set; }

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
                HlaNames = HlaNames,
                MatchingHla = MatchingHla
            };
        }

        public static DonorCosmosDocument FromRawInputDonor(RawInputDonor input)
        {
            return new DonorCosmosDocument
            {
                Id = input.DonorId.ToString(),
                DuplicateIdForOrdering = input.DonorId,
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
                DuplicateIdForOrdering = input.DonorId,
                RegistryCode = input.RegistryCode,
                DonorType = input.DonorType,
                HlaNames = input.ToRawInputDonor().HlaNames,
                MatchingHla = input.MatchingHla
            };
        }
    }
}