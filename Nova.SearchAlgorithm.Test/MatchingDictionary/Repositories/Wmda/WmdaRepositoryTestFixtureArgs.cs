using Nova.SearchAlgorithm.MatchingDictionary.Data;
using Nova.SearchAlgorithm.MatchingDictionary.Repositories;
using Nova.SearchAlgorithm.Test.MatchingDictionary.Data;

namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Repositories.Wmda
{
    public class WmdaRepositoryTestFixtureArgs
    {
        private static readonly IWmdaFileReader TestFileReader = new WmdaTestFileImporter();
        private static readonly IWmdaDataRepository Repo = new WmdaDataRepository(TestFileReader);
        private static readonly string[] MolecularLoci =  { "A*", "B*", "C*", "DQB1*", "DRB1*" };
        private static readonly string[] SerologyLoci = { "A", "B", "Cw", "DQ", "DR" };

        public static object[] HlaNomAllelesTestArgs = {
            new object[] { Repo.Alleles, MolecularLoci }
        };
        public static object[] HlaNomSerologiesTestArgs = {
            new object[] { Repo.Serologies, SerologyLoci }
        };
        public static object[] HlaNomPTestArgs = {
            new object[] { Repo.PGroups, MolecularLoci }
        };
        public static object[] HlaNomGTestArgs = {
            new object[] { Repo.GGroups, MolecularLoci }
        };
        public static object[] RelSerSerTestArgs = {
            new object[] { Repo.SerologyToSerologyRelationships, SerologyLoci }
        };
        public static object[] RelDnaSerTestArgs = {
            new object[] { Repo.DnaToSerologyRelationships, MolecularLoci }
        };
        public static object[] ConfidentialAllelesTestArgs = {
            new object[] { Repo.ConfidentialAlleles, MolecularLoci }
        };
    }
}
