using Atlas.Common.GeneticData.PhenotypeInfo;
using Atlas.MatchingAlgorithm.Client.Models;
using Atlas.MatchingAlgorithm.Client.Models.Donors;
using Atlas.MatchingAlgorithm.Common.Models;
using Atlas.MatchingAlgorithm.Data.Models.DonorInfo;
using Atlas.MatchingAlgorithm.Models;
using EnumStringValues;

namespace Atlas.MatchingAlgorithm.Extensions
{
    public static class SearchableDonorInformationExtensions
    {
        public static DonorInfo ToDonorInfo(this SearchableDonorInformation donor)
        {
            return new DonorInfo
            {
                DonorId = donor.DonorId,
                DonorType = donor.DonorType.ParseToEnum<DonorType>(),
                HlaNames = donor.HlaAsPhenotype()
            };
        }

        public static PhenotypeInfo<string> HlaAsPhenotype(this SearchableDonorInformation donor)
        {
            return new PhenotypeInfo<string>
            {
                A = { Position1 = donor.A_1, Position2 = donor.A_2 },
                B = { Position1 = donor.B_1, Position2 = donor.B_2 },
                C = { Position1 = donor.C_1, Position2 = donor.C_2 },
                Dpb1 = { Position1 = donor.DPB1_1, Position2 = donor.DPB1_2 },
                Dqb1 = { Position1 = donor.DQB1_1, Position2 = donor.DQB1_2 },
                Drb1 = { Position1 = donor.DRB1_1, Position2 = donor.DRB1_2 },
            };
        }
    }
}