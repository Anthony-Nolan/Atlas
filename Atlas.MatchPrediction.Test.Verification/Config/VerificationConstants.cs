using Atlas.Client.Models.Search;
using Atlas.DonorImport.Data.Models;
using Atlas.MatchPrediction.Test.Verification.Data.Models.Entities.TestHarness;

namespace Atlas.MatchPrediction.Test.Verification.Config
{
    internal static class VerificationConstants
    {
        public const int SearchLociCount = 5;
        
        private const DatabaseDonorType GenotypeDatabaseDonorType = DatabaseDonorType.Cord;
        private const DonorType GenotypeSearchDonorType = DonorType.Cord;

        private const DatabaseDonorType MaskedDatabaseDonorType = DatabaseDonorType.Adult;
        private const DonorType MaskedSearchDonorType = DonorType.Adult;

        public static DatabaseDonorType GetDatabaseDonorType(SimulatedHlaTypingCategory category)
        {
            return category == SimulatedHlaTypingCategory.Genotype
                ? GenotypeDatabaseDonorType
                : MaskedDatabaseDonorType;
        }

        public static DonorType GetSearchDonorType(SimulatedHlaTypingCategory category)
        {
            return category == SimulatedHlaTypingCategory.Genotype
                ? GenotypeSearchDonorType
                : MaskedSearchDonorType;
        }
    }
}