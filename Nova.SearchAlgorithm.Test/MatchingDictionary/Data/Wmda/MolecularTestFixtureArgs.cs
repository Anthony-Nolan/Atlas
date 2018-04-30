using Nova.SearchAlgorithm.MatchingDictionary.Models.Wmda.Filters;

namespace Nova.SearchAlgorithm.Test.MatchingDictionary.Data.Wmda
{
    public class MolecularTestFixtureArgs
    {
        public static object[] Args = {
            new object[] { MolecularFilter.Instance.Filter, new[] { "A*", "B*", "C*", "DQB1*", "DRB1*" } }
        };
    }
}
