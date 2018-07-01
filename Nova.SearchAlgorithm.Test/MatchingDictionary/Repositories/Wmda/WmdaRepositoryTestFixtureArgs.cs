using Nova.SearchAlgorithm.MatchingDictionary.Data;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using Nova.SearchAlgorithm.Test.MatchingDictionary.Data;

namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Repositories.Wmda
{
    public static class WmdaRepositoryTestFixtureArgs
    {
        private const string HlaDatabaseVersionToTest = "3310";
        private static readonly IWmdaFileReader TestFileReader = new WmdaTestFileImporter();
        private static readonly string[] MolecularLoci =  { "A*", "B*", "C*", "DQB1*", "DRB1*" };
        private static readonly string[] SerologyLoci = { "A", "B", "Cw", "DQ", "DR" };

        public static readonly IWmdaDataRepository WmdaDataRepository = new WmdaDataRepository(TestFileReader, HlaDatabaseVersionToTest);

        public static object[] HlaNomAllelesTestArgs = {
            new object[] { WmdaDataRepository.Alleles, MolecularLoci }
        };
        public static object[] HlaNomSerologiesTestArgs = {
            new object[] { WmdaDataRepository.Serologies, SerologyLoci }
        };
        public static object[] HlaNomPTestArgs = {
            new object[] { WmdaDataRepository.PGroups, MolecularLoci }
        };
        public static object[] HlaNomGTestArgs = {
            new object[] { WmdaDataRepository.GGroups, MolecularLoci }
        };
        public static object[] RelSerSerTestArgs = {
            new object[] { WmdaDataRepository.SerologyToSerologyRelationships, SerologyLoci }
        };
        public static object[] RelDnaSerTestArgs = {
            new object[] { WmdaDataRepository.AlleleToSerologyRelationships, MolecularLoci }
        };
        public static object[] ConfidentialAllelesTestArgs = {
            new object[] { WmdaDataRepository.ConfidentialAlleles, MolecularLoci }
        };
        public static object[] AllelesStatusesTestArgs = {
            new object[] { WmdaDataRepository.AlleleStatuses, MolecularLoci }
        };
        public static object[] AllelesNameHistoriesTestArgs = {
            new object[] { WmdaDataRepository.AlleleNameHistories, MolecularLoci }
        };
    }
}
